using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Audio;
using MisterGames.Common.Data;
using MisterGames.Common.Easing;
using MisterGames.Common.Labels;
using MisterGames.Common.Maths;
using MisterGames.Common.Tick;
using MisterGames.Logic.Clocks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Scripts.Runtime.Enemies.Clocks {
    
    public sealed class ClockAttackBehaviour : MonoBehaviour, IActorComponent, IUpdate {

        [Header("Timescale")]
        [SerializeField] private LabelValue _timeScalePriority;
        [SerializeField] [MinMaxSlider(0.1f, 3f)] private Vector2 _slowTimescale = new Vector2(0.5f, 0.75f);
        [SerializeField] [MinMaxSlider(0.1f, 3f)] private Vector2 _fastTimescale = new Vector2(1.25f, 2f);
        [SerializeField] private AnimationCurve _timescaleCurve = EasingType.Linear.ToAnimationCurve();

        [Header("Sounds")]
        [SerializeField] private HashId _soundId;
        [SerializeField] [Min(0f)] private float _volumeDuringAttack = 1f;
        [SerializeField] [Min(0f)] private float _volumeAfterAttackFinishedSlow = 0.3f;
        [SerializeField] [Min(0f)] private float _volumeAfterAttackFinishedFast = 0.6f;
        [SerializeField] [Min(0f)] private float _volumeSmoothing = 2f;
        [SerializeField] private int _startActionEveryNSecond = 8;
        [SerializeReference] [SubclassSelector] private IActorAction _tickAction;

        private CancellationTokenSource _enableCts;
        private CancellationTokenSource _cts;
        private IActor _actor;
        private ClockBehaviour _clockBehaviour;
        private Monster _monster;
        private AudioHandle _audioHandle;
        private bool _useSlowTimescale;
        private int _tickCounter;
        private float _currentVolume;
        private float _targetVolume;
        
        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
            _clockBehaviour = actor.GetComponent<ClockBehaviour>();
            _monster = actor.GetComponent<Monster>();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            
            _monster.OnMonsterEvent += OnMonsterEvent;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            
            PlayerLoopStage.UnscaledUpdate.Unsubscribe(this);
            
            _clockBehaviour.OnTick -= OnTick;
            _monster.OnMonsterEvent -= OnMonsterEvent;
            
            StopAttack();
        }

        private void OnMonsterEvent(MonsterEventType evt) {
            switch (evt) {
                case MonsterEventType.Death:
                case MonsterEventType.Respawn:
                    StopAttack();
                    break;
                
                case MonsterEventType.AttackStart:
                    AsyncExt.RecreateCts(ref _cts);
                    StartAttack(_monster.AttackDuration, _cts.Token).Forget();
                    break;
            }
        }

        void IUpdate.OnUpdate(float dt) {
            _currentVolume = _currentVolume.SmoothExpNonZero(_targetVolume, _volumeSmoothing, dt);
            _audioHandle.Volume = _currentVolume;
        }

        private async UniTask StartAttack(float duration, CancellationToken cancellationToken) {
            PlayerLoopStage.UnscaledUpdate.Subscribe(this);
            
            _clockBehaviour.OnTick -= OnTick;
            _clockBehaviour.OnTick += OnTick;
            
            _useSlowTimescale = !_useSlowTimescale;
            _currentVolume = _audioHandle.Volume;
            _targetVolume = _volumeDuringAttack;
            
            float tsStart = Time.timeScale;
            float ts = _useSlowTimescale ? _slowTimescale.GetRandomInRange() : _fastTimescale.GetRandomInRange();
            int priority = _timeScalePriority.GetValue();
            
            float speed = duration > 0f ? 1f / duration : float.MaxValue;
            float t = 0f;
            
            while (!cancellationToken.IsCancellationRequested && t < 1f) {
                t = Mathf.Clamp01(t + speed * Time.deltaTime);
                TimescaleSystem.SetTimeScale(priority, Mathf.Lerp(tsStart, ts, _timescaleCurve.Evaluate(t)));
                
                await UniTask.Yield();
            }

            _targetVolume = Time.timeScale > 1f ? _volumeAfterAttackFinishedFast : _volumeAfterAttackFinishedSlow;
        }

        private void StopAttack() {
            AsyncExt.DisposeCts(ref _cts);
            PlayerLoopStage.UnscaledUpdate.Unsubscribe(this);
            
            _clockBehaviour.OnTick -= OnTick;
            _audioHandle.Release();
            _tickCounter = 0;
            
            TimescaleSystem.RemoveTimeScale(_timeScalePriority.GetValue());
        }
        
        private void OnTick(int second) {
            if (_cts == null || _tickCounter++ % _startActionEveryNSecond != 0) return;
            
            _audioHandle.Release();
            
            _tickAction?.Apply(_actor, _enableCts.Token).Forget();
            
            _audioHandle = AudioPool.Main.GetAudioHandle(_actor.Transform, _soundId);
            _audioHandle.Volume = _currentVolume;
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo;
        
        private void OnDrawGizmos() {
            if (!_showDebugInfo) return;
            
            Handles.Label(transform.TransformPoint(Vector3.forward * 0.2f), $"Timescale {Time.timeScale:0.000}");
        }
#endif
    }
    
}