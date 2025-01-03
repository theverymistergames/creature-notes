using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Logic.Damage;
using MisterGames.Tick.Core;
using MisterGames.Tweens;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterFx : MonoBehaviour, IActorComponent, IUpdate {
        
        [Header("Progress")]
        [SerializeField] private ProgressAction[] _progressActions;
        
        [Header("Actions")]
        [SerializeReference] [SubclassSelector] private IActorAction _onRespawn;
        [SerializeReference] [SubclassSelector] private IActorAction _onDeath;
        [SerializeReference] [SubclassSelector] private IActorAction _onArmed;
        [SerializeReference] [SubclassSelector] private IActorAction _onAttackStarted;
        [SerializeReference] [SubclassSelector] private IActorAction _onAttackPerformed;
        
        [Serializable]
        private struct ProgressAction {
            [Min(0f)] public float progressSmoothing;
            public AnimationCurve progressCurve;
            [SerializeReference] [SubclassSelector] public IProgressModulator progressModulator;
            [SerializeReference] [SubclassSelector] public ITweenProgressAction progressAction;
            public TweenEvent[] events;
        }

        private float[] _progressArray;
        private CancellationTokenSource _enableCts;
        private IActor _actor;
        private Monster _monster;
        private HealthBehaviour _health;
        private MonsterData _monsterData;
        
        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
            _health = actor.GetComponent<HealthBehaviour>();
            _monster = actor.GetComponent<Monster>();
            _progressArray = new float[_progressActions?.Length ?? 0];
        }
        
        void IActorComponent.OnSetData(IActor actor) {
            _monsterData = actor.GetData<MonsterData>();
        }
        
        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            PlayerLoopStage.Update.Subscribe(this);
            
            _health.OnRestoreFullHealth += OnRestoreHealth;
            _health.OnDamage += OnDamage;
            
            _monster.OnArmed += OnArmed;
            _monster.OnAttackStarted += OnAttackStarted;
            _monster.OnAttackPerformed += OnAttackPerformed;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            PlayerLoopStage.Update.Unsubscribe(this);
            
            _health.OnRestoreFullHealth -= OnRestoreHealth;
            _health.OnDamage -= OnDamage;
            
            _monster.OnArmed -= OnArmed;
            _monster.OnAttackStarted -= OnAttackStarted;
            _monster.OnAttackPerformed -= OnAttackPerformed;
        }

        void IUpdate.OnUpdate(float dt) {
            float progress = _monster.Progress;

            for (int i = 0; i < _progressActions.Length; i++) {
                ref var action = ref _progressActions[i];
                ref float p = ref _progressArray[i];

                float targetProgress = action.progressCurve?.Evaluate(progress) ?? progress;
                targetProgress = action.progressModulator?.Modulate(targetProgress) ?? targetProgress;
                
                float oldP = p;
                p = p.SmoothExpNonZero(targetProgress, action.progressSmoothing * dt);
                
                action.progressAction?.OnProgressUpdate(p);
                action.events.NotifyTweenEvents(_actor, p, oldP, _enableCts.Token);
            }
        }
        
        private void OnRestoreHealth() {
            _monsterData.respawnSound.Apply(_actor, destroyCancellationToken).Forget();
            _onRespawn?.Apply(_actor, destroyCancellationToken).Forget();
        }

        private void OnDamage(DamageInfo info) {
            if (!info.mortal) return;

            _monsterData.deathSound.Apply(_actor, destroyCancellationToken).Forget();
            _onDeath?.Apply(_actor, destroyCancellationToken).Forget();
        }

        private void OnArmed() {
            _monsterData.armSound.Apply(_actor, destroyCancellationToken).Forget();
            _onArmed?.Apply(_actor, destroyCancellationToken).Forget();
        }
        
        private void OnAttackStarted() {
            _monsterData.startAttackSound.Apply(_actor, destroyCancellationToken).Forget();
            _onAttackStarted?.Apply(_actor, destroyCancellationToken).Forget();
        }

        private void OnAttackPerformed() {
            _monsterData.performAttackSound.Apply(_actor, destroyCancellationToken).Forget();
            _onAttackPerformed?.Apply(_actor, destroyCancellationToken).Forget();
        }
    }
    
}