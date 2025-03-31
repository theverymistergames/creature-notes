using System.Collections.Generic;
using MisterGames.Character.Phys;
using MisterGames.Collisions.Core;
using MisterGames.Common;
using MisterGames.Common.Labels;
using MisterGames.Common.Maths;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshMaterialDetector : MaterialDetectorBase {
        
        [SerializeField] private CapsuleCollider _capsuleCollider;
        [SerializeField] private CollisionDetectorBase _groundDetector;
        [SerializeField] private FleshPositionSampler _fleshPositionSampler;
        [SerializeField] private LabelValue _material;
        [SerializeField] [Min(0f)] private float _weightMin = 0.1f;
        [SerializeField] [Min(0f)] private float _weightMax = 1f;
        [SerializeField] private float _topPointOffset;
        
        private readonly List<MaterialInfo> _materialList = new();
        private Transform _transform;
        
        private void Awake() {
            _transform = _capsuleCollider.transform;
        }

        public override IReadOnlyList<MaterialInfo> GetMaterials() {
            _materialList.Clear();
            
            var up = _transform.up;
            float halfHeight = _capsuleCollider.height * 0.5f;
            
            var lowPoint = _transform.TransformPoint(_capsuleCollider.center) - halfHeight * up;
            var point = lowPoint;
            
#if UNITY_EDITOR
            if (_showDebugInfo) DebugExt.DrawSphere(point, 0.01f, Color.yellow);
#endif

            if (!_fleshPositionSampler.TrySamplePosition(ref point)) {
                return _materialList;
            }

#if UNITY_EDITOR
            if (_showDebugInfo) {
                DebugExt.DrawCircle(point, _transform.rotation, 0.05f, Color.green);
                DebugExt.DrawRay(point, _transform.up * 0.005f, Color.green);
            }
#endif

            float mag = VectorUtils.SignedMagnitudeOfProject(point - lowPoint, up);
            float diff = halfHeight + _topPointOffset;  
            float weight = mag > 0f 
                ? Mathf.Lerp(_weightMin, _weightMax, diff > 0f ? Mathf.Clamp01(mag / diff) : 1f) 
                : 0f;

            if (weight > 0f) _materialList.Add(new MaterialInfo(_material.GetValue(), weight));
            
            return _materialList;
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo;

        private void OnValidate() {
            if (_weightMax < _weightMin) _weightMax = _weightMin;
        }
#endif
    }
    
}