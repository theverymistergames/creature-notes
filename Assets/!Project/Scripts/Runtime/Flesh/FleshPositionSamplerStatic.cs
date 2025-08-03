using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshPositionSamplerStatic : FleshPositionSamplerBase {

        [SerializeField] private FleshVertexPosition[] _fleshVertexPositions;
        [SerializeField] [Min(0f)] private float _maxDistance = 1f;

        public override bool TrySamplePosition(ref Vector3 point) {
            float minSqrDistance = float.MaxValue;
            var resultPoint = point;

            for (int i = 0; i < _fleshVertexPositions.Length; i++) {
                var p = point;

                if (!_fleshVertexPositions[i].TrySamplePosition(ref p) ||
                    (point - p).sqrMagnitude is var sqrDistance &&
                    (sqrDistance > minSqrDistance || sqrDistance > _maxDistance * _maxDistance)) {
                    continue;
                }

                minSqrDistance = sqrDistance;
                resultPoint = p;
            }

            point = resultPoint;
            return minSqrDistance < float.MaxValue;
        }
    }
    
}