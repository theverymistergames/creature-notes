using MisterGames.Character.Phys;
using MisterGames.Collisions.Core;
using MisterGames.Common;
using MisterGames.Common.Labels;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshMaterialDetector : MaterialDetectorBase {
        
        [SerializeField] private CapsuleCollider _capsuleCollider;
        [SerializeField] private CollisionDetectorBase _groundDetector;
        [SerializeField] private FleshPositionSampler _fleshPositionSampler;
        [SerializeField] private LabelValue _priority;
        [SerializeField] private LabelValue _material;

        private Transform _transform;
        
        private void Awake() {
            _transform = _capsuleCollider.transform;
        }

        public override bool TryGetMaterial(out int materialId, out int priority) {
            materialId = _material.GetValue();
            priority = _priority.GetValue();
            
            var up = _transform.up;
            var point = _transform.TransformPoint(_capsuleCollider.center) - _capsuleCollider.height * 0.5f * up;

#if UNITY_EDITOR
            if (_showDebugInfo) DebugExt.DrawSphere(point, 0.01f, Color.yellow);
#endif
            
            if (!_fleshPositionSampler.TrySamplePosition(ref point)) return false;

#if UNITY_EDITOR
            if (_showDebugInfo) {
                DebugExt.DrawCircle(point, _transform.rotation, 0.05f, Color.green);
                DebugExt.DrawRay(point, _transform.up * 0.005f, Color.green);
            }
#endif

            return !_groundDetector.HasContact || Vector3.Dot(point - _groundDetector.CollisionInfo.point, up) > 0f;
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo;
#endif
    }
    
}