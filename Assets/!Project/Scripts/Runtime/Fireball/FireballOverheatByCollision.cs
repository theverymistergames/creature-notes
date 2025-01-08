using MisterGames.Actors;
using MisterGames.Collisions.Rigidbodies;
using MisterGames.Collisions.Utils;
using UnityEngine;

namespace _Project.Scripts.Runtime.Fireball {
    
    public sealed class FireballOverheatByCollision : MonoBehaviour, IActorComponent {

        [SerializeField] private CollisionEmitter _collisionEmitter;

        private void OnEnable() {
            _collisionEmitter.CollisionEnter += CollisionEnter;
        }

        private void OnDisable() {
            _collisionEmitter.CollisionEnter -= CollisionEnter;
        }

        private void CollisionEnter(Collision collision) {
            if (collision.GetComponentFromCollision<IActor>() is not { } actor ||
                !actor.TryGetComponent(out FireballShootingBehaviour fireballShootingBehaviour)) 
            {
                return;
            }
            
            fireballShootingBehaviour.ForceStage(FireballShootingBehaviour.Stage.Overheat);
        }
    }
    
}
