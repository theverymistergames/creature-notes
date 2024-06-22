using System;
using MisterGames.Common.Attributes;
using MisterGames.Common.GameObjects;
using MisterGames.Common.Maths;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Runtime.Telescope {
    
    public sealed class StarGroupsCanvas : MonoBehaviour {

        [SerializeField] private StarGroupsData _starGroupsData;
        [SerializeField] private Image _referenceImage;
        [SerializeField] private RectTransform _canvas;
        [SerializeField] private RectTransform _starPrefab;
        [SerializeField] private Vector3 _starScale = Vector3.one;
        [SerializeField] [Min(-1)] private int _selectedIndex = -1; 
        [SerializeField] private bool _placeSelectedGroupInCenter = true; 
        [SerializeField] private StarGroup[] _starGroups = Array.Empty<StarGroup>();
        
        [Serializable]
        private struct StarGroup {
            public bool enabled;
            
            public Vector3 rotationWhenCentered;
            public float scaleWhenCentered;
            
            public RectTransform[] stars;
            public RectTransform[] links;
            
            public TransformData[] initialStarsData;
            public TransformData[] initialLinksData;
        }

        public int StarGroupCount => (_starGroups?.Length ?? 0);
        public int SelectedStarGroupIndex => _selectedIndex;

        private void Awake() {
            SelectDefaultStarGroup();
        }

        public void EnableAllStarGroups() {
            for (int i = 0; i < (_starGroups?.Length ?? 0); i++) {
                SetStarGroupEnabled(i, isEnabled: true);
            }
        }
        
        public void DisableAllStarGroups() {
            for (int i = 0; i < (_starGroups?.Length ?? 0); i++) {
                SetStarGroupEnabled(i, isEnabled: false);
            }
        }

        public void SetStarGroupEnabled(int index, bool isEnabled) {
            if (index < 0 || _starGroups == null || index >= _starGroups.Length) return;
            
            var starGroup = _starGroups[index];
            
            for (int j = 0; j < starGroup.stars.Length; j++) {
                var s = starGroup.stars[j];
                if (s == null) continue;
                
                s.gameObject.SetActive(isEnabled);
            }
            
            for (int j = 0; j < starGroup.links.Length; j++) {
                var s = starGroup.links[j];
                if (s == null) continue;
                
                s.gameObject.SetActive(isEnabled);
            }
        }

        public void SelectDefaultStarGroup() {
            SelectStarGroup(_selectedIndex);
        }

        public void SelectStarGroup(int index) {
            _selectedIndex = index;
            
            for (int i = 0; i < (_starGroups?.Length ?? 0); i++) {
                PlaceStarGroup(i, inCenter: i == index);
                SetStarGroupEnabled(i, isEnabled: i == index || index < 0);
            }
        }

        private void PlaceStarGroup(int index, bool inCenter) {
            if (index < 0 || _starGroups == null || index >= _starGroups.Length || _canvas == null || _referenceImage == null) return;
            
            var starGroup = _starGroups[index];
            var center = StarGroupUtils.GetStarGroupCenter(starGroup.initialStarsData);
            var canvasCenter = GetCanvasCenter();
            
            for (int j = 0; j < starGroup.stars.Length; j++) {
                if (j >= (starGroup.initialStarsData?.Length ?? 0)) break;
                
                var t = starGroup.stars[j];
                if (t == null) continue;
                
                var initialStarData = starGroup.initialStarsData[j];
                initialStarData = initialStarData.WithScale(initialStarData.scale.Multiply(_starScale));
                
                if (!inCenter) {
                    t.localPosition = initialStarData.position;
                    t.localRotation = initialStarData.rotation;
                    t.localScale = initialStarData.scale;
                    
                    continue;
                }

                var d = StarGroupUtils.GetTransformDataPlacedInCenter(
                    initialStarData,
                    rotationOffset: Quaternion.Euler(starGroup.rotationWhenCentered),
                    starGroup.scaleWhenCentered,
                    starGroup.scaleWhenCentered,
                    center
                );

                t.localPosition = d.position + canvasCenter;
                t.localRotation = d.rotation;
                t.localScale = d.scale;
            }
            
            for (int j = 0; j < starGroup.links.Length; j++) {
                if (j >= (starGroup.initialLinksData?.Length ?? 0)) break;
                
                var t = starGroup.links[j];
                if (t == null) continue;
                
                var initialLinkData = starGroup.initialLinksData[j];
                initialLinkData = initialLinkData.WithScale(initialLinkData.scale.Multiply(_starScale));
                
                if (!inCenter) {
                    t.localPosition = initialLinkData.position;
                    t.localRotation = initialLinkData.rotation;
                    t.localScale = initialLinkData.scale;
                    continue;
                }
                
                var d = StarGroupUtils.GetTransformDataPlacedInCenter(
                    initialLinkData,
                    rotationOffset: Quaternion.Euler(starGroup.rotationWhenCentered),
                    starGroup.scaleWhenCentered,
                    starGroup.scaleWhenCentered,
                    center
                );

                t.localPosition = d.position + canvasCenter;
                t.localRotation = d.rotation;
                t.localScale = d.scale;
            }
        }

        private Vector3 GetCanvasCenter() {
            return _referenceImage.rectTransform.localPosition;
        }
     
#if UNITY_EDITOR
        private void SetupStarPrefabs() {
            if (_starPrefab == null || _canvas == null) return;
            
            UnityEditor.Undo.RecordObject(gameObject, "SetupStarPrefabs");
            
            for (int i = 0; i < (_starGroups?.Length ?? 0); i++) {
                ref var starGroup = ref _starGroups[i];

                for (int j = starGroup.initialStarsData?.Length ?? 0; j < (starGroup.stars?.Length ?? 0); j++) {
                    var s = starGroup.stars[j];
                    if (s != null) UnityEditor.Undo.DestroyObjectImmediate(s.gameObject);
                }
                
                if (starGroup.stars == null) starGroup.stars = new RectTransform[starGroup.initialStarsData?.Length ?? 0];
                else Array.Resize(ref starGroup.stars, starGroup.initialStarsData?.Length ?? 0);
                
                for (int j = 0; j < (starGroup.stars?.Length ?? 0); j++) {
                    ref var s = ref starGroup.stars[j];
                    if (s != null) continue;

                    s = (RectTransform) UnityEditor.PrefabUtility.InstantiatePrefab(_starPrefab, _canvas);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(s, "SetupStarPrefabs");
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
        
        [Button]
        private void ApplyInitialLayout() {
            UnityEditor.Undo.RecordObject(gameObject, "ApplyInitialLayout");
            
            SetupStarPrefabs();
            EnableAllStarGroups();
            SelectStarGroup(-1);
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        [Button]
        private void SaveInitialLayout() {
            if (_selectedIndex >= 0) {
                Debug.LogWarning($"{nameof(StarGroupsCanvas)}: cannot save initial layout when selected index is non negative");
                return;
            }
            
            UnityEditor.Undo.RecordObject(gameObject, "SaveAsInitialLayout");

            for (int i = 0; i < (_starGroups?.Length ?? 0); i++) {
                ref var starGroup = ref _starGroups[i];
                
                starGroup.initialStarsData = new TransformData[starGroup.stars?.Length ?? 0];
                starGroup.initialLinksData = new TransformData[starGroup.links?.Length ?? 0];
                
                for (int j = 0; j < (starGroup.stars?.Length ?? 0); j++) {
                    var s = starGroup.stars[j];
                    starGroup.initialStarsData[j] = new TransformData(s.localPosition, s.localRotation, s.localScale.Divide(_starScale));
                }
                
                for (int j = 0; j < (starGroup.links?.Length ?? 0); j++) {
                    var element = starGroup.links[j];
                    starGroup.initialLinksData[j] = new TransformData(element.localPosition, element.localRotation, element.localScale.Divide(_starScale));
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
        
        [Button]
        private void SaveLayoutIntoSourceData() {
            if (_starGroupsData == null) return;
            
            UnityEditor.Undo.RecordObject(_starGroupsData, "WriteLayoutIntoSourceData");

            if (_starGroupsData.starGroups == null) {
                _starGroupsData.starGroups = new StarGroupsData.StarGroup[_starGroups?.Length ?? 0];
            }
            else {
                Array.Resize(ref _starGroupsData.starGroups, _starGroups?.Length ?? 0);   
            }
            
            for (int i = 0; i < (_starGroups?.Length ?? 0); i++) {
                if (i >= (_starGroupsData.starGroups?.Length ?? 0)) break;
                
                ref var localGroup = ref _starGroups[i];
                ref var sourceGroup = ref _starGroupsData.starGroups[i];

                sourceGroup.canvasRotation = localGroup.rotationWhenCentered;
                sourceGroup.canvasScale = localGroup.scaleWhenCentered;
                
                sourceGroup.stars = new TransformData[localGroup.initialStarsData?.Length ?? 0];
                sourceGroup.links = new TransformData[localGroup.initialLinksData?.Length ?? 0];

                if (localGroup.initialStarsData != null) {
                    Array.Copy(localGroup.initialStarsData, sourceGroup.stars, localGroup.initialStarsData.Length);    
                }
                
                if (localGroup.initialLinksData != null) {
                    Array.Copy(localGroup.initialLinksData, sourceGroup.links, localGroup.initialLinksData.Length);    
                }
            }

            UnityEditor.EditorUtility.SetDirty(_starGroupsData);
        }
        
        [Button]
        private void ReadLayoutFromSourceData() {
            if (_starGroupsData == null) return;
            
            UnityEditor.Undo.RecordObject(gameObject, "ReadLayoutFromSourceData");
            
            if (_starGroups == null) {
                _starGroups = new StarGroup[_starGroupsData.starGroups?.Length ?? 0];
            }
            else {
                Array.Resize(ref _starGroups, _starGroupsData.starGroups?.Length ?? 0);   
            }

            for (int i = 0; i < (_starGroupsData.starGroups?.Length ?? 0); i++) {
                if (i >= (_starGroups?.Length ?? 0)) break;

                ref var sourceGroup = ref _starGroupsData.starGroups[i];
                ref var localGroup = ref _starGroups[i];

                localGroup.rotationWhenCentered = sourceGroup.canvasRotation;
                localGroup.scaleWhenCentered = sourceGroup.canvasScale;

                localGroup.initialStarsData = new TransformData[sourceGroup.stars?.Length ?? 0];
                localGroup.initialLinksData = new TransformData[sourceGroup.links?.Length ?? 0];

                if (sourceGroup.stars != null) {
                    Array.Copy(sourceGroup.stars, localGroup.initialStarsData, sourceGroup.stars.Length);
                }

                if (sourceGroup.links != null) {
                    Array.Copy(sourceGroup.links, localGroup.initialLinksData, sourceGroup.links.Length);
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
            
            SetupStarPrefabs();
            EnableAllStarGroups();
            SelectDefaultStarGroup();
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        private void OnValidate() {
            SetupStarPrefabs();
            SelectDefaultStarGroup();
        }
#endif
    }
    
}