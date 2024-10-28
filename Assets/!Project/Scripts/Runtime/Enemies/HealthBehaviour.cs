using System;
using MisterGames.Actors;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class HealthBehaviour : MonoBehaviour, IActorComponent {

        public event Action<HealthBehaviour, DamageInfo> OnDamage = delegate { };
        public event Action<HealthBehaviour> OnDeath = delegate { };
        
        public float Health { get; private set; }
        public bool IsAlive => Health > 0f;
        public bool IsDead => Health <= 0f;
        
        private HealthData _healthData;

        void IActorComponent.OnSetData(IActor actor) {
            _healthData = actor.GetData<HealthData>();
        }

        private void Start() {
            RestoreFullHealth();
        }

        public void RestoreFullHealth() {
            Health = _healthData.health;
        }
        
        public DamageInfo TakeDamage(float damage) {
            float oldHealth = Health;
            Health = Mathf.Max(0f, Health - damage);
            
            float damageTotal = oldHealth - Health;  
            bool mortal = Health <= 0;
            
            var info = new DamageInfo(damageTotal, mortal);
            OnDamage.Invoke(this, info);
            
            if (mortal) OnDeath.Invoke(this);
            
            return info;
        }
    }
    
}