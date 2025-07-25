using System.Collections.Generic;
using MisterGames.Collisions.Rigidbodies;
using MisterGames.Common.Layers;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshPositionSamplerByTrigger : FleshPositionSamplerBase {

        [SerializeField] private TriggerEmitter _triggerEmitter;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] [Min(0f)] private float _maxDistance = 1f;
        
        private readonly HashSet<FleshVertexPosition> _fleshVertexPositions = new();
        
        private void OnEnable() {
            _triggerEmitter.TriggerEnter += TriggerEnter;
            _triggerEmitter.TriggerExit += TriggerExit;
        }

        private void OnDisable() {
            _triggerEmitter.TriggerEnter -= TriggerEnter;
            _triggerEmitter.TriggerExit -= TriggerExit;
        }

        public override bool TrySamplePosition(ref Vector3 point) {
            float minSqrDistance = float.MaxValue;
            var resultPoint = point;
            
            foreach (var fleshVertexPosition in _fleshVertexPositions) {
                var p = point;
                
                if (!fleshVertexPosition.TrySamplePosition(ref p) ||
                    (point - p).sqrMagnitude is var sqrDistance &&
                    (sqrDistance > minSqrDistance || sqrDistance > _maxDistance * _maxDistance))
                {
                    continue;
                }
                
                minSqrDistance = sqrDistance;
                resultPoint = p;
            }

            point = resultPoint;
            return minSqrDistance < float.MaxValue;
        }

        private void TriggerEnter(Collider collider) {
            if (!_layerMask.Contains(collider.gameObject.layer) ||
                !collider.TryGetComponent(out FleshVertexPosition fleshVertexPosition)) 
            {
                return;
            }

            _fleshVertexPositions.Add(fleshVertexPosition);
        }

        private void TriggerExit(Collider collider) {
            if (collider == null || 
                !_layerMask.Contains(collider.gameObject.layer) ||
                !collider.TryGetComponent(out FleshVertexPosition fleshVertexPosition)) 
            {
                return;
            }

            _fleshVertexPositions.Remove(fleshVertexPosition);
        }
    }
    
}