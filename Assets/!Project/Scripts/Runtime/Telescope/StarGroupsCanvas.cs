using System;
using MisterGames.Common.Attributes;
using MisterGames.Common.GameObjects;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Runtime.Telescope {
    
    public sealed class StarGroupsCanvas : MonoBehaviour {

        [SerializeField] private StarGroupsData _starGroupsData;
        [SerializeField] private Image _referenceImage;
        [SerializeField] [Min(-1)] private int _selectedIndex = -1; 
        [SerializeField] private StarGroup[] _starGroups;
        
        [Serializable]
        private struct StarGroup {
            
            public Vector3 rotationWhenCentered;
            public float scaleWhenCentered;
            
            public RectTransform[] stars;
            public RectTransform[] links;
            
            [HideInInspector] 
            public TransformData[] initialStarsData;
            [HideInInspector] 
            public TransformData[] initialLinksData;
        }

        public int StarGroupCount => _starGroups.Length;

        public void EnableAllStarGroups() {
            _selectedIndex = -1;
            
            for (int i = 0; i < _starGroups.Length; i++) {
                PlaceStarGroup(i, inCenter: false);
                SetStarGroupEnabled(i, isEnabled: true);
            }
        }
        
        public void DisableAllStarGroups() {
            for (int i = 0; i < _starGroups.Length; i++) {
                SetStarGroupEnabled(i, isEnabled: false);
            }
        }
        
        public void SelectDefaultStarGroup() {
            SelectStarGroup(_selectedIndex);
        }
        
        public void SelectStarGroup(int index) {
            _selectedIndex = index;
            
            for (int i = 0; i < _starGroups.Length; i++) {
                PlaceStarGroup(i, inCenter: i == index);
                SetStarGroupEnabled(i, isEnabled: i == index || index < 0);
            }
        }

        private void SetStarGroupEnabled(int index, bool isEnabled) {
            var starGroup = _starGroups[index];
            
            for (int j = 0; j < starGroup.stars.Length; j++) {
                starGroup.stars[j].gameObject.SetActive(isEnabled);
            }
            
            for (int j = 0; j < starGroup.links.Length; j++) {
                starGroup.links[j].gameObject.SetActive(isEnabled);
            }
        }

        private void PlaceStarGroup(int index, bool inCenter) {
            var starGroup = _starGroups[index];
            var center = StarGroupUtils.GetStarGroupCenter(starGroup.initialStarsData);
            
            for (int j = 0; j < starGroup.stars.Length; j++) {
                if (j >= (starGroup.initialStarsData?.Length ?? 0)) break;
                
                var t = starGroup.stars[j];

                if (!inCenter) {
                    t.position = starGroup.initialStarsData[j].position;
                    t.rotation = starGroup.initialStarsData[j].rotation;
                    t.localScale = starGroup.initialStarsData[j].scale;
                    
                    continue;
                }

                var d = StarGroupUtils.GetTransformDataPlacedInCenter(
                    starGroup.initialStarsData[j],
                    rotationOffset: Quaternion.Euler(starGroup.rotationWhenCentered),
                    starGroup.scaleWhenCentered,
                    starGroup.scaleWhenCentered,
                    center
                );
                
                t.position = d.position + GetCanvasCenter();
                t.rotation = d.rotation;
                t.localScale = d.scale;
            }
            
            for (int j = 0; j < starGroup.links.Length; j++) {
                if (j >= (starGroup.initialLinksData?.Length ?? 0)) break;
                
                var t = starGroup.links[j];

                if (!inCenter) {
                    t.position = starGroup.initialLinksData[j].position;
                    t.rotation = starGroup.initialLinksData[j].rotation;
                    t.localScale = starGroup.initialLinksData[j].scale;
                    continue;
                }
                
                var d = StarGroupUtils.GetTransformDataPlacedInCenter(
                    starGroup.initialLinksData[j],
                    rotationOffset: Quaternion.Euler(starGroup.rotationWhenCentered),
                    starGroup.scaleWhenCentered,
                    starGroup.scaleWhenCentered,
                    center
                );

                t.position = d.position + GetCanvasCenter();
                t.rotation = d.rotation;
                t.localScale = d.scale;
            }
        }

        private Vector3 GetCanvasCenter() {
            return _referenceImage.rectTransform.position;
        }
     
#if UNITY_EDITOR
        [Button]
        private void ApplyInitialLayout() {
            EnableAllStarGroups();
        }

        [Button]
        private void SaveInitialLayout() {
            if (_selectedIndex >= 0) return;
            
            UnityEditor.Undo.RecordObject(gameObject, "SaveAsInitialLayout");
            
            for (int i = 0; i < _starGroups.Length; i++) {
                ref var starGroup = ref _starGroups[i];
                
                starGroup.initialStarsData = new TransformData[starGroup.stars.Length];
                starGroup.initialLinksData = new TransformData[starGroup.links.Length];
                
                for (int j = 0; j < starGroup.stars.Length; j++) {
                    var element = starGroup.stars[j];
                    starGroup.initialStarsData[j] = new TransformData(element.position, element.rotation, element.localScale);
                }
                
                for (int j = 0; j < starGroup.links.Length; j++) {
                    var element = starGroup.links[j];
                    starGroup.initialLinksData[j] = new TransformData(element.position, element.rotation, element.localScale);
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
        
        [Button]
        private void SaveLayoutIntoSourceData() {
            UnityEditor.Undo.RecordObject(_starGroupsData, "WriteLayoutIntoSourceData");

            int oldCount = _starGroupsData.starGroups.Length;
            
            Array.Resize(ref _starGroupsData.starGroups, _starGroups.Length);
            
            for (int i = 0; i < _starGroupsData.starGroups.Length; i++) {
                ref var dest = ref _starGroupsData.starGroups[i];
                ref var source = ref _starGroups[i];

                dest.canvasRotation = source.rotationWhenCentered;
                dest.canvasScale = source.scaleWhenCentered;
                
                dest.stars = new TransformData[source.initialStarsData.Length];
                dest.links = new TransformData[source.initialLinksData.Length];
                
                for (int j = 0; j < source.initialStarsData.Length; j++) {
                    dest.stars[j] = source.initialStarsData[j];
                }
                
                for (int j = 0; j < source.initialLinksData.Length; j++) {
                    dest.links[j] = source.initialLinksData[j];
                }
            }

            for (int i = oldCount; i < _starGroupsData.starGroups.Length; i++) {
                ref var dest = ref _starGroupsData.starGroups[i];

                dest.groupScale = 1f;
                dest.starScale = 1f;
                dest.telescopeDistance = 10f;
            }

            UnityEditor.EditorUtility.SetDirty(_starGroupsData);
        }
        
        [Button]
        private void ReadLayoutFromSourceData() {
            UnityEditor.Undo.RecordObject(gameObject, "ReadLayoutFromSourceData");
            
            for (int i = 0; i < _starGroups.Length; i++) {
                if (i >= (_starGroupsData.starGroups?.Length ?? 0)) break;
                
                ref var dest = ref _starGroups[i];
                ref var source = ref _starGroupsData.starGroups[i];

                dest.rotationWhenCentered = source.canvasRotation;
                dest.scaleWhenCentered = source.canvasScale;

                dest.initialStarsData = source.stars;
                dest.initialLinksData = source.links;
            }
            
            EnableAllStarGroups();
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        private void OnValidate() {
            if (_selectedIndex >= 0) SelectDefaultStarGroup();
        }
#endif
    }
    
}