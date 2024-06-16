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
        private struct StarGroup {
            public Vector3 orientation;
            public List<Transform> stars;
        }

        public int StarGroupCount => _starGroups.Length;
        
        public IReadOnlyList<Transform> GetStars(int groupIndex) {
            return groupIndex >= 0 && groupIndex < _starGroups.Length
                ? _starGroups[groupIndex].stars
                : Array.Empty<Transform>();
        }

        private void PlaceStarGroups() {
            if (_center == null || _starGroupsData == null) return;

            var centerPos = _center.position;
            _starGroups ??= Array.Empty<StarGroup>();

            for (int i = 0; i < _starGroups.Length; i++) {
                ref var starGroup = ref _starGroups[i];
                ref var source = ref _starGroupsData.starGroups[i];

                var rotationOffset = Quaternion.Euler(_rotationOffset) * Quaternion.Euler(starGroup.orientation);
                var canvasGroupCenter = StarGroupUtils.GetStarGroupCenter(source.stars);
                var worldGroupCenter = centerPos + rotationOffset * Vector3.forward * _distance;

                for (int j = 0; j < starGroup.stars.Count; j++) {
                    var s = starGroup.stars[j];
                    
                    var centeredData = StarGroupUtils.GetTransformDataPlacedInCenter(
                        source.stars[j].WithScale(Vector3.one),
                        Quaternion.Euler(source.canvasRotation),
                        _starScale * _distance,
                        _groupScale * _distance,
                        canvasGroupCenter
                    );
                    
#if UNITY_EDITOR
                    if (!Application.isPlaying) Undo.RecordObject(s, $"{nameof(StarGroupsPlacer)}.PlaceStarGroups");
#endif
                    
                    s.SetPositionAndRotation(worldGroupCenter + centeredData.position, centeredData.rotation);
                    s.localScale = centeredData.scale;
                }
            }
        }

#if UNITY_EDITOR
        [Button]
        private void SaveLayoutIntoSourceData() {
            _starGroupsData.telescopeDistance = _distance;
            _starGroupsData.starScale = _starScale;
            _starGroupsData.groupScale = _groupScale;
            
            for (int i = 0; i < _starGroupsData.starGroups.Length; i++) {
                if (i >= (_starGroups?.Length ?? 0)) break;
                
                ref var dest = ref _starGroupsData.starGroups[i];
                ref var source = ref _starGroups[i];

                dest.telescopeOrientation = source.orientation;
            }
            
            EditorUtility.SetDirty(_starGroupsData);
        }
        
        [Button]
        private void LoadLayoutFromSourceData() {
            if (Application.isPlaying) return;
            
            _distance = _starGroupsData.telescopeDistance;
            _starScale = _starGroupsData.starScale;
            _groupScale = _starGroupsData.groupScale;
            
            Array.Resize(ref _starGroups, _starGroupsData.starGroups.Length);
            
            for (int i = 0; i < _starGroupsData.starGroups.Length; i++) {
                ref var source = ref _starGroupsData.starGroups[i];
                ref var dest = ref _starGroups[i];

                dest.orientation = source.telescopeOrientation;
                dest.stars ??= new List<Transform>();
                
                for (int j = dest.stars.Count - 1; j >= 0; j--) {
                    if (dest.stars[j] == null) dest.stars.RemoveAt(j);
                }
                
                for (int j = dest.stars.Count - 1; j >= source.stars.Length; j--) {
                    DestroyImmediate(dest.stars[j]);
                    dest.stars.RemoveAt(j);
                }
                
                for (int j = dest.stars.Count; j < source.stars.Length; j++) {
                    dest.stars.Add((Transform) PrefabUtility.InstantiatePrefab(_starGroupsData.starPrefab, transform));
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