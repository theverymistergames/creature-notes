using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Common.Async;
using MisterGames.Common.Maths;
using MisterGames.Logic.Damage;
using MisterGames.Scenario.Events;
using MisterGames.Tick.Core;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class Monster : MonoBehaviour, IActorComponent, IUpdate {

        public event Action OnArmed = delegate { };
        public event Action OnAttackStarted = delegate { };
        public event Action OnAttackPerformed = delegate { };
        
        public bool IsDead => _health.IsDead;
        public bool IsArmed => Progress >= 1f;
        public float Progress { get; private set; }

        private CancellationTokenSource _enableCts;
        
        private IActor _actor;
        private HealthBehaviour _health;
        private MonsterData _monsterData;

        private Vector2 _attackCooldownRange;
        private float _progressSpeed;
        private float _nextAttackTime;
        
        void IActorComponent.OnAwake(IActor actor) {
            _health = actor.GetComponent<HealthBehaviour>();
        }

        void IActorComponent.OnSetData(IActor actor) {
            _monsterData = actor.GetData<MonsterData>();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            PlayerLoopStage.Update.Subscribe(this);
            
            _health.OnDamage += OnDamage;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            PlayerLoopStage.Update.Unsubscribe(this);
            
            _health.OnDamage -= OnDamage;
        }

        public void Respawn(float armDuration, Vector2 attackCooldownRange) {
            _health.RestoreFullHealth();
            _progressSpeed = armDuration > 0f ? 1f / armDuration : float.MaxValue;
            _attackCooldownRange = attackCooldownRange;
        }

        public void Kill(bool notifyDamage = true) {
            _health.Kill(notifyDamage);
            
            if (notifyDamage) return;
            
            _progressSpeed = 0f;
            Progress = 0f;
        }

        void IUpdate.OnUpdate(float dt) {
            bool wasArmed = IsArmed;
            Progress = Mathf.Clamp01(Progress + dt * _progressSpeed);
            
            if (_health.IsDead) return;
            
            if (!wasArmed && IsArmed) OnArmed.Invoke();

            float time = Time.time;
            if (IsArmed && time >= _nextAttackTime) {
                _nextAttackTime = time + _attackCooldownRange.GetRandomInRange();
                PerformAttack(_enableCts.Token).Forget();
            }
        }
        
        private void OnDamage(HealthBehaviour health, DamageInfo info) {
            if (!info.mortal) return;

            _progressSpeed = _monsterData.deathDuration > 0f ? -1f / _monsterData.deathDuration : float.MinValue;
        }

        private async UniTask PerformAttack(CancellationToken cancellationToken) {
            OnAttackStarted.Invoke();
            
            await UniTask.Delay(TimeSpan.FromSeconds(_monsterData.attackDelay), cancellationToken: cancellationToken)
                .SuppressCancellationThrow();
            
            if (cancellationToken.IsCancellationRequested) return;
            
            _monsterData.debuffEvent.SetCount(_monsterData.debuffType.value);
            OnAttackPerformed.Invoke();
        }
    }
    
}