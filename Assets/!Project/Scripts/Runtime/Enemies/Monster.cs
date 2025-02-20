using System;
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
        [SerializeField] [Range(0f, 1f)] private float _progress;

        public delegate void EventCallback(MonsterEventType evt);
        public event EventCallback OnMonsterEvent = delegate { };
        
        public int TypeId => _monsterType.GetValue();
        public bool IsDead => _health.IsDead;
        public bool IsArmed => Progress >= 1f;
        public float Progress => _progress;
        public float AttackDuration { get; private set; }

        private CancellationTokenSource _enableCts;
        
        private IActor _actor;
        private HealthBehaviour _health;
        private MonsterData _monsterData;

        private Vector2 _attackDurationRange;
        private Vector2 _attackCooldownRange;
        private float _progressSpeed;
        private float _nextAttackTime;
        private byte _attackId;
        
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

        public void Respawn(float armDuration, Vector2 attackDurationRange, Vector2 attackCooldownRange) {
            if (_health.IsAlive) return;
            
            _health.RestoreFullHealth();
            _progressSpeed = armDuration > 0f ? 1f / armDuration : float.MaxValue;
            _attackDurationRange = attackDurationRange;
            _attackCooldownRange = attackCooldownRange;
            _nextAttackTime = 0f;
            _attackId++;
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"Monster[{name}].Respawn: f {Time.frameCount}, " +
                                          $"arm duration {armDuration:0.00}, " +
                                          $"attack duration {attackDurationRange.x:0.00} - {attackDurationRange.y:0.00}, " +
                                          $"attack cooldown {attackCooldownRange.x:0.00} - {attackCooldownRange.y:0.00}");
#endif
            
            OnMonsterEvent.Invoke(MonsterEventType.Respawn);
        }

        public void Kill(bool notifyDamage = true) {
            if (_health.IsDead) return;
            
            _health.Kill(notifyDamage: notifyDamage);
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"Monster[{name}].Kill: f {Time.frameCount}, notifyDamage {notifyDamage}");
#endif
            
            if (notifyDamage) return;

            _progress = 0f;
            _progressSpeed = 0f;
            _attackId++;
        }

        void IUpdate.OnUpdate(float dt) {
            bool wasArmed = IsArmed;
            _progress = Mathf.Clamp01(_progress + dt * _progressSpeed);
            
            if (_health.IsDead) return;

            if (!wasArmed && IsArmed) {
#if UNITY_EDITOR
                if (_showDebugInfo) Debug.Log($"Monster[{name}].OnUpdate: f {Time.frameCount}, armed");
#endif                
                
                OnMonsterEvent.Invoke(MonsterEventType.Arm);
            }

            float time = Time.time;
            
            if (IsArmed && time >= _nextAttackTime) {
                float attackDuration = _attackDurationRange.GetRandomInRange();
                float attackCooldown = _monsterData.allowMultipleAttacks ? _attackCooldownRange.GetRandomInRange() : float.MaxValue;
                _nextAttackTime = time + _monsterData.attackDelay + attackDuration + attackCooldown;
                
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
            
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken)
                .SuppressCancellationThrow();
            
            if (cancellationToken.IsCancellationRequested || id != _attackId) return;
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"Monster[{name}].PerformAttack: f {Time.frameCount}, attack #{_attackId} started");
#endif
            
            OnMonsterEvent.Invoke(MonsterEventType.AttackStart);
            
            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken)
                         .SuppressCancellationThrow();
            
            if (cancellationToken.IsCancellationRequested || id != _attackId) return;
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"Monster[{name}].PerformAttack: f {Time.frameCount}, attack #{_attackId} finished");
#endif      
            
            OnMonsterEvent.Invoke(MonsterEventType.AttackFinish);
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

        [Button(mode: ButtonAttribute.Mode.Runtime)] 
        private void RespawnTest() {
            Respawn(_armDurationTest, _attackDurationRangeTest, _attackCooldownRangeTest);
        }
#endif
    }
    
}