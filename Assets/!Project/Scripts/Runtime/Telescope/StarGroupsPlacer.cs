using System;
using System.Collections.Generic;
using MisterGames.Common.Attributes;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Runtime.Telescope {
    
    public sealed class StarGroupsPlacer : MonoBehaviour {

        [SerializeField] private StarGroupsData _starGroupsData;
        [SerializeField] private Transform _center;
        [SerializeField] private Vector3 _rotationOffset;
        [SerializeField] private float _distance;
        [SerializeField] private float _starScale;
        [SerializeField] private float _groupScale;
        [SerializeField] private StarGroup[] _starGroups;
        
        [Serializable]
        public struct StarGroup {
            public Vector3 canvasRotation;
            public Vector3 orientation;
            public List<Transform> stars;
        }

        public Transform Center => _center;
        public IReadOnlyList<StarGroup> StarGroups => _starGroups;

        private void Awake() {
            PlaceStarGroups();
        }

        private void PlaceStarGroups() {
            if (_center == null || _starGroupsData == null) return;

            var centerPos = _center.position;
            _starGroups ??= Array.Empty<StarGroup>();

            for (int i = 0; i < _starGroups.Length; i++) {
                if (i >= (_starGroupsData.starGroups?.Length ?? 0)) break;
                
                ref var starGroup = ref _starGroups[i];
                ref var source = ref _starGroupsData.starGroups[i];

                var rotationOffset = Quaternion.Euler(_rotationOffset) * Quaternion.Euler(starGroup.orientation);
                var canvasGroupCenter = StarGroupUtils.GetStarGroupCenter(source.stars);
                var worldGroupCenter = centerPos + rotationOffset * Vector3.forward * _distance;

                for (int j = 0; j < starGroup.stars.Count; j++) {
                    if (j >= (source.stars?.Length ?? 0)) continue;
                    
                    var s = starGroup.stars[j];
                    if (s == null) continue;
                    
                    var centeredData = StarGroupUtils.GetTransformDataPlacedInCenter(
                        source.stars[j].WithScale(Vector3.one),
                        Quaternion.Euler(starGroup.canvasRotation),
                        _starScale * _distance,
                        _groupScale * _distance,
                        canvasGroupCenter
                    );
                    
#if UNITY_EDITOR
                    if (!Application.isPlaying) Undo.RecordObject(s, $"{nameof(StarGroupsPlacer)}.PlaceStarGroups");
#endif
                    
                    s.SetPositionAndRotation(
                        worldGroupCenter + rotationOffset * centeredData.position,
                        centeredData.rotation
                    );
                    
                    s.localScale = centeredData.scale;
                }
            }
        }

#if UNITY_EDITOR
        [Button]
        private void SaveLayoutIntoSourceData() {
            if (Application.isPlaying || _starGroupsData == null) return;
            
            _starGroupsData.telescopeDistance = _distance;
            _starGroupsData.starScale = _starScale;
            _starGroupsData.groupScale = _groupScale;
            
            for (int i = 0; i < (_starGroups?.Length ?? 0); i++) {
                if (i >= (_starGroupsData.starGroups?.Length ?? 0)) break;
                
                ref var sourceGroup = ref _starGroupsData.starGroups[i];
                ref var localGroup = ref _starGroups[i];

                sourceGroup.telescopeOrientation = localGroup.orientation;
                sourceGroup.canvasRotation = localGroup.canvasRotation;
            }
            
            EditorUtility.SetDirty(_starGroupsData);
        }
        
        [Button]
        private void LoadLayoutFromSourceData() {
            if (Application.isPlaying || _starGroupsData == null) return;

            _distance = _starGroupsData.telescopeDistance;
            _starScale = _starGroupsData.starScale;
            _groupScale = _starGroupsData.groupScale;
            
            Array.Resize(ref _starGroups, _starGroupsData.starGroups?.Length ?? 0);
            
            for (int i = 0; i < (_starGroupsData.starGroups?.Length ?? 0); i++) {
                if (i >= (_starGroups?.Length ?? 0)) break;
                
                ref var sourceGroup = ref _starGroupsData.starGroups[i];
                ref var localGroup = ref _starGroups[i];

                localGroup.orientation = sourceGroup.telescopeOrientation;
                localGroup.canvasRotation = sourceGroup.canvasRotation;
                localGroup.stars ??= new List<Transform>();
                
                for (int j = localGroup.stars.Count - 1; j >= 0; j--) {
                    if (localGroup.stars[j] == null) localGroup.stars.RemoveAt(j);
                }
                
                for (int j = localGroup.stars.Count - 1; j >= sourceGroup.stars.Length; j--) {
                    DestroyImmediate(localGroup.stars[j].gameObject);
                    localGroup.stars.RemoveAt(j);
                }
                
                for (int j = localGroup.stars.Count; j < sourceGroup.stars.Length; j++) {
                    localGroup.stars.Add((Transform) PrefabUtility.InstantiatePrefab(_starGroupsData.starPrefab, transform));
                }
            }
            
            PlaceStarGroups();
        }

        private void OnValidate() {
            PlaceStarGroups();
        }
#endif
    }

}