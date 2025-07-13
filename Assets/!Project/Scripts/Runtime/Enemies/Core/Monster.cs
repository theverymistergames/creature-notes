using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Labels;
using MisterGames.Common.Maths;
using MisterGames.Logic.Damage;
using MisterGames.Common.Tick;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class Monster : MonoBehaviour, IActorComponent, IUpdate {

        [SerializeField] private LabelValue _monsterType;
        [SerializeField] private bool _affectedByTimeScale = true;
        [SerializeField] [Range(0f, 1f)] private float _progress;

        public delegate void EventCallback(MonsterEventType evt);
        public event EventCallback OnMonsterEvent = delegate { };
        
        public int TypeId => _monsterType.GetValue();
        public bool IsDead => _health.IsDead;
        public bool IsArmed => Progress >= 1f;
        public float Progress => _progress;
        public float TargetProgress => _progressSpeed > 0f ? 1f : 0f;
        public float AttackDuration { get; private set; }
        public int Difficulty { get; private set; }
        
        private CancellationTokenSource _enableCts;
        
        private IActor _actor;
        private HealthBehaviour _health;
        private MonsterData _monsterData;

        private Vector2 _attackDurationRange;
        private Vector2 _attackCooldownRange;
        private float _progressSpeed;
        private float _timer;
        private byte _attackId;
        
        void IActorComponent.OnAwake(IActor actor) {
            _health = actor.GetComponent<HealthBehaviour>();
        }

        void IActorComponent.OnSetData(IActor actor) {
            _monsterData = actor.GetData<MonsterData>();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            PlayerLoopStage.UnscaledUpdate.Subscribe(this);
            
            _health.OnDamage += OnDamage;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            PlayerLoopStage.UnscaledUpdate.Unsubscribe(this);
            
            _health.OnDamage -= OnDamage;
        }

        public void Respawn(float armDuration, Vector2 attackDurationRange, Vector2 attackCooldownRange, int difficulty) {
            if (_health.IsAlive) return;
            
            _health.RestoreFullHealth();
            _progressSpeed = armDuration > 0f ? 1f / armDuration : float.MaxValue;
            _attackDurationRange = attackDurationRange;
            _attackCooldownRange = attackCooldownRange;
            _timer = 0f;
            _attackId++;
            Difficulty = difficulty;
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"Monster[{name}].Respawn: f {Time.frameCount}, " +
                                          $"arm duration {armDuration:0.00}, " +
                                          $"attack duration {attackDurationRange.x:0.00} - {attackDurationRange.y:0.00}, " +
                                          $"attack cooldown {attackCooldownRange.x:0.00} - {attackCooldownRange.y:0.00}");
#endif
            
            OnMonsterEvent.Invoke(MonsterEventType.Respawn);
        }

        public void Kill(bool instantReset) {
            if (instantReset) {
#if UNITY_EDITOR
                if (_showDebugInfo) Debug.Log($"Monster[{name}].Kill: f {Time.frameCount}, instant reset");
#endif
                
                _health.Kill(notifyDamage: false);
            
                OnMonsterEvent.Invoke(MonsterEventType.Reset);

                _progress = 0f;
                _progressSpeed = 0f;
                _attackId++;
                
                return;
            }
            
            if (_health.IsDead) return;

#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"Monster[{name}].Kill: f {Time.frameCount}, notify death");
#endif
            
            _health.Kill(notifyDamage: true);
        }

        void IUpdate.OnUpdate(float dt) {
            dt *= _affectedByTimeScale || Time.timeScale <= 0f ? Time.timeScale : 1f;
            
            bool wasArmed = IsArmed;
            
            _progress = Mathf.Clamp01(_progress + dt * _progressSpeed);
            _timer = Mathf.Max(0f, _timer - dt);
            
            if (_health.IsDead) return;

            if (!wasArmed && IsArmed) {
#if UNITY_EDITOR
                if (_showDebugInfo) Debug.Log($"Monster[{name}].OnUpdate: f {Time.frameCount}, armed");
#endif                
                
                OnMonsterEvent.Invoke(MonsterEventType.Arm);
            }
            
            if (IsArmed && _timer <= 0f) {
                float attackDuration = _attackDurationRange.GetRandomInRange();
                float attackCooldown = _monsterData.allowMultipleAttacks ? _attackCooldownRange.GetRandomInRange() : float.MaxValue;
                _timer = _monsterData.attackDelay + attackDuration + attackCooldown;
                
                PerformAttack(_monsterData.attackDelay, attackDuration, _enableCts.Token).Forget();
            }
        }
        
        private void OnDamage(DamageInfo info) {
            if (!info.mortal) return;

#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"Monster[{name}].OnDamage: f {Time.frameCount}, mortal true");
#endif
            
            _progressSpeed = _monsterData.deathDuration > 0f ? -1f / _monsterData.deathDuration : float.MinValue;
            _attackId++;
            
            OnMonsterEvent.Invoke(MonsterEventType.Death);
        }

        private async UniTask PerformAttack(float delay, float duration, CancellationToken cancellationToken) {
            byte id = ++_attackId;
            
            AttackDuration = duration;
            OnMonsterEvent.Invoke(MonsterEventType.AttackPrepare);

            await Delay(delay, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested || id != _attackId) return;
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"Monster[{name}].PerformAttack: f {Time.frameCount}, attack #{_attackId} started");
#endif
            
            OnMonsterEvent.Invoke(MonsterEventType.AttackStart);
            
            await Delay(duration, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested || id != _attackId) return;
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"Monster[{name}].PerformAttack: f {Time.frameCount}, attack #{_attackId} finished");
#endif      
            
            OnMonsterEvent.Invoke(MonsterEventType.AttackFinish);
        }

        private async UniTask Delay(float duration, CancellationToken cancellationToken) {
            float t = 0f;
            float speed = duration > 0f ? 1f / duration : float.MaxValue;
            
            while (t < 1f && !cancellationToken.IsCancellationRequested) {
                float dt = _affectedByTimeScale || Time.timeScale <= 0f ? Time.deltaTime : Time.unscaledDeltaTime;
                t += dt * speed;

                await UniTask.Yield();
            }
        }

        public override string ToString() {
            return $"{nameof(Monster)}({_monsterType})";
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo;
        [SerializeField] private float _armDurationTest = 1f;
        [SerializeField] private Vector2 _attackDurationRangeTest = Vector2.one;
        [SerializeField] private Vector2 _attackCooldownRangeTest = Vector2.one;
        [SerializeField] [Min(0)] private int _difficultyTest = 0;

        [Button(mode: ButtonAttribute.Mode.Runtime)] 
        private void RespawnTest() {
            Respawn(_armDurationTest, _attackDurationRangeTest, _attackCooldownRangeTest, _difficultyTest);
        }
#endif
    }
    
}