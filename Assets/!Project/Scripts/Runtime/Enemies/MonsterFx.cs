using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Logic.Damage;
using MisterGames.Tick.Core;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterFx : MonoBehaviour, IActorComponent {

        private IActor _actor;
        private HealthBehaviour _health;
        private Monster _monster;
        private MonsterData _monsterData;
        
        void IActorComponent.OnAwake(IActor actor) {
            _health = actor.GetComponent<HealthBehaviour>();
            _monster = actor.GetComponent<Monster>();
        }

        void IActorComponent.OnSetData(IActor actor) {
            _monsterData = actor.GetData<MonsterData>();
        }

        private void OnEnable() {
            _health.OnRestoreFullHealth += OnRestoreHealth;
            _health.OnDamage += OnDamage;
            
            _monster.OnArmed += OnArmed;
            _monster.OnAttackStarted += OnAttackStarted;
            _monster.OnAttackPerformed += OnAttackPerformed;
        }

        private void OnDisable() {
            _health.OnRestoreFullHealth -= OnRestoreHealth;
            _health.OnDamage -= OnDamage;
            
            _monster.OnArmed -= OnArmed;
            _monster.OnAttackStarted -= OnAttackStarted;
            _monster.OnAttackPerformed -= OnAttackPerformed;
        }

        private void OnRestoreHealth() {
            _monsterData.respawnSound.Apply(_actor, destroyCancellationToken).Forget();
        }

        private void OnDamage(DamageInfo info) {
            if (!info.mortal) return;

            _monsterData.deathSound.Apply(_actor, destroyCancellationToken).Forget();
        }

        private void OnArmed() {
            _monsterData.armSound.Apply(_actor, destroyCancellationToken).Forget();
        }
        
        private void OnAttackStarted() {
            _monsterData.startAttackSound.Apply(_actor, destroyCancellationToken).Forget();
        }

        private void OnAttackPerformed() {
            _monsterData.performAttackSound.Apply(_actor, destroyCancellationToken).Forget();
        }
    }
    
}