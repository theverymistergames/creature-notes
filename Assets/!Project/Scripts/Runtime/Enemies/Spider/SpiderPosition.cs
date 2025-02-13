using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using UnityEngine;

namespace _Project.Scripts.Runtime.Spider {
    
    public sealed class SpiderPosition : MonoBehaviour {
        
        [SerializeField] private SpiderWebPlacer _spiderWebPlacer;
        [SerializeField] private Transform _target;

        private void OnEnable() {
            _spiderWebPlacer.OnWebUpdated += OnWebUpdated;
        }

        private void OnDisable() {
            _spiderWebPlacer.OnWebUpdated -= OnWebUpdated;
        }

        private void OnWebUpdated() {
            var centers = _spiderWebPlacer.WebCenters;
            if (centers.Count == 0) return;

            var normals = _spiderWebPlacer.WebNormals;
            
            var pos = centers[0];
            var up = normals.Count > 0 ? normals[0] : Vector3.up; 
            var rot = Quaternion.LookRotation(RandomExtensions.OnUnitCircle(up), up);
            
            _target.SetPositionAndRotation(pos, rot);
        }

#if UNITY_EDITOR
        [Button]
        private void ApplyPosition() {
            if (_target == null || _spiderWebPlacer == null) return;
            
            OnWebUpdated();
        }
#endif
    }
    
}