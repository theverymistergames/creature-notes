using MisterGames.Actors;
using MisterGames.Logic.Damage;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class Monster : MonoBehaviour, IActorComponent {

        public bool IsDead => _health.IsDead;

        private IActor _actor;
        private HealthBehaviour _health;
        
        void IActorComponent.OnAwake(IActor actor) {
            _health = actor.GetComponent<HealthBehaviour>();
        }

        public void Kill(bool instant = false) {
            _health.Kill(notifyDamage: !instant);
        }

        public void Respawn() {
            _health.RestoreFullHealth();
        }
    }
    
}