using System;
using MisterGames.Actors;
using MisterGames.Collisions.Rigidbodies;
using MisterGames.Collisions.Utils;
using MisterGames.Common.Layers;
using MisterGames.Logic.Damage;
using UnityEngine;

namespace _Project.Scripts.Runtime.Fireball {
    
    public sealed class DamageOnCollision : MonoBehaviour, IActorComponent {
        
        [SerializeField] private CollisionEmitter _collisionEmitter;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private DamageAuthor _damageAuthor;
        [SerializeField] private bool _applyOnlyOnce;
        [SerializeField] [Min(0f)] private float _damage;

        private IActor _actor;
        private bool _didDamage;

        private enum DamageAuthor {
            Actor,
            ParentActor,
        }
        
        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
        }

        private void OnEnable() {
            _collisionEmitter.CollisionEnter += CollisionEnter;
        }

        private void OnDisable() {
            _didDamage = false;
            _collisionEmitter.CollisionEnter -= CollisionEnter;
        }

        private void CollisionEnter(Collision collision) {
            if (_didDamage && _applyOnlyOnce ||
                !_layerMask.Contains(collision.gameObject.layer) ||
                collision.GetComponentFromCollision<IActor>() is not { } actor ||
                !actor.TryGetComponent(out HealthBehaviour healthBehaviour)
            ) {
                return;
            }

            _didDamage = true;
            
            var author = _damageAuthor switch {
                DamageAuthor.Actor => _actor,
                DamageAuthor.ParentActor => _actor.ParentActor,
                _ => throw new ArgumentOutOfRangeException(),
            };
            
            healthBehaviour.TakeDamage(_damage, author, collision.GetContact(0).point);
        }
    }
    
}