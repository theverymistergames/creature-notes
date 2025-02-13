using MisterGames.Collisions.Rigidbodies;
using MisterGames.Collisions.Utils;
using UnityEngine;

namespace _Project.Scripts.Runtime.Spider {
    
    public sealed class SpiderWebBurner : MonoBehaviour {
        
        [SerializeField] private TriggerEmitter _triggerEmitter;

        private void OnEnable() {
            _triggerEmitter.TriggerEnter += TriggerEnter;
        }

        private void OnDisable() {
            _triggerEmitter.TriggerEnter -= TriggerEnter;
        }

        private void TriggerEnter(Collider collider) {
            if (collider.GetComponentFromCollider<SpiderWebLine>() is {} line) line.Burn();
        }
    }
    
}