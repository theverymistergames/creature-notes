using System.Collections.Generic;
using MisterGames.Collisions.Core;
using MisterGames.Collisions.Detectors;
using MisterGames.Common;
using MisterGames.Common.Labels;
using MisterGames.Common.Maths;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshMaterialDetector : MaterialDetectorBase {
        
        [SerializeField] private FleshPositionSamplerBase _fleshPositionSampler;
        [SerializeField] private LabelValue _material;
        [SerializeField] [Min(0f)] private float _weightMin = 0.1f;
        [SerializeField] [Min(0f)] private float _weightMax = 1f;
        [SerializeField] [Min(0f)] private float _maxDistance = 0.5f;
        
        private readonly List<MaterialInfo> _materialList = new();

        public override IReadOnlyList<MaterialInfo> GetMaterials(Vector3 point, Vector3 normal) {
            _materialList.Clear();
            
#if UNITY_EDITOR
            if (_showDebugInfo) DebugExt.DrawSphere(point, 0.01f, Color.yellow);
#endif

            var topPoint = point;
            
            if (!_fleshPositionSampler.TrySamplePosition(ref topPoint)) {
                return _materialList;
            }

#if UNITY_EDITOR
            if (_showDebugInfo) {
                DebugExt.DrawCircle(point, Quaternion.identity, 0.05f, Color.green);
                DebugExt.DrawRay(point, normal * 0.005f, Color.green);
            }
#endif

            float mag = VectorUtils.SignedMagnitudeOfProject(topPoint - point, normal);
            float weight = mag > 0f 
                ? Mathf.Lerp(_weightMin, _weightMax, _maxDistance > 0f ? Mathf.Clamp01(mag / _maxDistance) : 1f) 
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