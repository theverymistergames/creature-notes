using System;
using System.Collections.Generic;
using MisterGames.Common.Attributes;
using MisterGames.Common.GameObjects;
using MisterGames.Common.Maths;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Telescope {
    
    public sealed class StarGroupsCanvas : MonoBehaviour {

        [SerializeField] private StarGroupsData _starGroupsData;
        [SerializeField] private Image _referenceImage;
        [SerializeField] private RectTransform _canvas;
        
        [Header("Stars")]
        [SerializeField] [Min(-1)] private int _selectedIndex = -1; 
        [SerializeField] private RectTransform _starPrefab;
        [SerializeField] private Vector3 _starScale = Vector3.one;
        [SerializeField] private float _randomizeScale;
        [SerializeField] private Vector3 _randomizeRotation;
        [SerializeField] [Range(0f, 1f)] private float _randomMultiplier;
        
        [Header("Lines")]
        [SerializeField] private LineRenderer _linkPrefab;
        [SerializeField] private float _linkOffset;
        [SerializeField] private float _linkWidth;
        [SerializeField] private CustomLink[] _customLinks = Array.Empty<CustomLink>();
        
        [ReadOnly]
        [SerializeField] private LineRenderer[] _customLines;

        [Header("Groups")]
        [SerializeField] private StarGroup[] _starGroups = Array.Empty<StarGroup>();

        [Header("Debug")]
        [SerializeField] private bool _enableEditMode;
        
        [Serializable]
        public struct CustomLink {
            public Vector2Int a;
            public Vector2Int b;
        }
        
        [Serializable]
        private struct StarGroup {
            public Vector3 rotationWhenCentered;
            public float scaleWhenCentered;
            
            public RectTransform[] stars;
            public LineRenderer[] links;
            
            public TransformData[] initialStarsData;
            public TransformData[] offsets;
            
            public int[] disableLinks;
        }

        public int StarGroupCount => _starGroups?.Length ?? 0;
        public int SelectedStarGroupIndex => _selectedIndex;
        public IReadOnlyList<CustomLink> CustomLinks => _customLinks ?? Array.Empty<CustomLink>();
        
        private void Awake() {
            SelectDefaultStarGroup();
        }

        public IReadOnlyList<RectTransform> GetGroupStars(int group) {
            if (group < 0 || group >= (_starGroups?.Length ?? 0)) return Array.Empty<RectTransform>();
            return _starGroups[group].stars;
        }

        public IReadOnlyList<LineRenderer> GetGroupLines(int group) {
            if (group < 0 || group >= (_starGroups?.Length ?? 0)) return Array.Empty<LineRenderer>();
            return _starGroups[group].links;
        }
        
        public IReadOnlyList<LineRenderer> GetCustomLines() {
            return _customLines;
        }
        
        public LineRenderer GetCustomLine(int index) {
            if (index < 0 || index >= (_customLines?.Length ?? 0)) return null;
            return _customLines[index];
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

                if (j < (starGroup.offsets?.Length ?? 0)) {
                    var offset = starGroup.offsets[j];
                    
                    initialStarData = initialStarData
                        .WithScale(initialStarData.scale + offset.scale * _randomMultiplier)
                        .WithRotation(initialStarData.rotation * Quaternion.Slerp(Quaternion.identity, offset.rotation, _randomMultiplier));
                }

                if (inCenter) {
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
                else {
                    t.localPosition = initialStarData.position;
                    t.localRotation = initialStarData.rotation;
                    t.localScale = initialStarData.scale;
                }

                int l = j - 1;
                if (l < 0 || l >= (starGroup.links?.Length ?? 0)) continue;
                
                var line = starGroup.links![l];
                if (line == null) continue;

                line.enabled = true;
                
                var p0 = t.position;
                var p1 = starGroup.stars[l].position;
                
                line.SetPosition(0, p0 + (p1 - p0).normalized * _linkOffset);
                line.SetPosition(1, p1 + (p0 - p1).normalized * _linkOffset);

                line.widthMultiplier = _linkWidth;
            }
            
            for (int i = 0; i < (starGroup.disableLinks?.Length ?? 0); i++) {
                int idx = starGroup.disableLinks![i];
                if (idx < 0 || idx >= (starGroup.links?.Length ?? 0)) continue;
                
                starGroup.links![idx].enabled = false;
            }
        }

        private Vector3 GetCanvasCenter() {
            return _referenceImage.rectTransform.localPosition;
        }
     
#if UNITY_EDITOR
        [Button]
        private void RandomizeStarTransforms() {
            UnityEditor.Undo.RecordObject(gameObject, "RandomizeStarTransforms");
            
            for (int i = 0; i < (_starGroups?.Length ?? 0); i++) {
                ref var starGroup = ref _starGroups[i];

                if (starGroup.offsets == null) starGroup.offsets = new TransformData[starGroup.initialStarsData?.Length ?? 0];
                else Array.Resize(ref starGroup.offsets, starGroup.initialStarsData?.Length ?? 0);

                for (int j = 0; j < (starGroup.offsets?.Length ?? 0); j++) {
                    ref var d = ref starGroup.offsets[j];
                    
                    d.scale = Random.Range(-_randomizeScale, _randomizeScale) * Vector3.one;
                    d.rotation = Quaternion.Euler(
                        Random.Range(-_randomizeRotation.x, _randomizeRotation.x),
                        Random.Range(-_randomizeRotation.y, _randomizeRotation.y),
                        Random.Range(-_randomizeRotation.z, _randomizeRotation.y)
                    );
                }
            }
            
            SetupStarPrefabs();
            SelectDefaultStarGroup();
            SetupCustomLinks();
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
        
        private void SetupStarPrefabs() {
            if (_starPrefab == null || _canvas == null) return;

            bool changed = false;
            int groupsCount = _starGroups?.Length ?? 0;
            
            UnityEditor.Undo.RecordObject(gameObject, "SetupStarPrefabs");
            
            for (int i = 0; i < groupsCount; i++) {
                ref var starGroup = ref _starGroups![i];

                int targetCount = starGroup.initialStarsData?.Length ?? 0;
                int currentCount = starGroup.stars?.Length ?? 0;
                
                for (int j = targetCount; j < currentCount; j++) {
                    var s = starGroup.stars![j];
                    if (s == null) continue;
                    
                    UnityEditor.Undo.DestroyObjectImmediate(s.gameObject);
                    changed = true;
                }

                if (starGroup.stars == null) {
                    starGroup.stars = new RectTransform[targetCount];
                    changed = true;
                }
                
                if (currentCount != targetCount) {
                    Array.Resize(ref starGroup.stars, targetCount);
                    changed = true;
                }
                
                for (int j = 0; j < targetCount; j++) {
                    ref var s = ref starGroup.stars[j];
                    if (s != null) continue;

                    s = (RectTransform) UnityEditor.PrefabUtility.InstantiatePrefab(_starPrefab, _canvas);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(s.gameObject, "SetupStarPrefabs");
                    changed = true;
                }

                int targetLinksCount = targetCount - 1;
                int currentLinksCount = starGroup.links?.Length ?? 0;
                
                for (int j = targetLinksCount; j >= 0 && j < currentLinksCount; j++) {
                    var s = starGroup.links![j];
                    if (s == null) continue;
                    
                    UnityEditor.Undo.DestroyObjectImmediate(s.gameObject);
                    changed = true;
                }

                if (targetLinksCount <= 0 || _linkPrefab == null) continue;

                if (starGroup.links == null) {
                    starGroup.links = new LineRenderer[targetLinksCount];
                    changed = true;
                }

                if (starGroup.links.Length != targetLinksCount) {
                    Array.Resize(ref starGroup.links, targetLinksCount);
                    changed = true;
                }
                
                for (int j = 0; j < targetLinksCount; j++) {
                    ref var s = ref starGroup.links[j];
                    if (s != null) continue;

                    s = (LineRenderer) UnityEditor.PrefabUtility.InstantiatePrefab(_linkPrefab, _canvas);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(s.gameObject, "SetupStarPrefabs");
                    changed = true;
                }
            }

            if (changed) {
                UnityEditor.EditorUtility.SetDirty(gameObject);
            }
        }
        
        [Button]
        private void ApplyInitialLayout() {
            UnityEditor.Undo.RecordObject(gameObject, "ApplyInitialLayout");
            
            SetupStarPrefabs();
            EnableAllStarGroups();
            SelectStarGroup(-1);
            SetupCustomLinks();
            
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
                
                for (int j = 0; j < (starGroup.stars?.Length ?? 0); j++) {
                    var s = starGroup.stars[j];
                    starGroup.initialStarsData[j] = new TransformData(s.localPosition, s.localRotation, s.localScale.Divide(_starScale));
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

                if (localGroup.initialStarsData != null) {
                    Array.Copy(localGroup.initialStarsData, sourceGroup.stars, localGroup.initialStarsData.Length);    
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

                if (sourceGroup.stars != null) {
                    Array.Copy(sourceGroup.stars, localGroup.initialStarsData, sourceGroup.stars.Length);
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
            
            SetupStarPrefabs();
            EnableAllStarGroups();
            SelectDefaultStarGroup();
            SetupCustomLinks();
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }

        private void SetupCustomLinks() {
            if (_starGroups == null) return;
            
            UnityEditor.Undo.RecordObject(gameObject, "SetupCustomLinks");

            bool changed = false;
            int customLinksCount = _customLinks?.Length ?? 0;
            int customLinesCount = _customLines?.Length ?? 0;
            
            for (int i = customLinksCount; i < customLinesCount; i++) {
                var l = _customLines![i];
                if (l == null) continue;
                
                UnityEditor.Undo.DestroyObjectImmediate(l.gameObject);
                changed = true;
            }

            if (_customLines == null) {
                _customLines = new LineRenderer[customLinksCount];
                changed = true;
            }

            if (customLinesCount != customLinksCount) {
                Array.Resize(ref _customLines, customLinksCount);
                changed = true;
            }
            
            for (int i = 0; i < customLinksCount; i++) {
                ref var customLink = ref _customLinks![i];
                
                if (customLink.a.x < 0 || customLink.a.x >= (_starGroups?.Length ?? 0) ||
                    customLink.b.x < 0 || customLink.b.x >= (_starGroups?.Length ?? 0) || 
                    _linkPrefab == null
                ) {
                    continue;
                }

                var groupA = _starGroups![customLink.a.x];
                var groupB = _starGroups[customLink.b.x];
                
                if (customLink.a.y < 0 || customLink.a.y >= (groupA.stars?.Length ?? 0) ||
                    customLink.b.y < 0 || customLink.b.y >= (groupB.stars?.Length ?? 0)
                ) {
                    continue;
                }

                ref var s = ref _customLines[i];

                if (s == null) {
                    s = (LineRenderer) UnityEditor.PrefabUtility.InstantiatePrefab(_linkPrefab, _canvas);
                    UnityEditor.Undo.RegisterCreatedObjectUndo(s.gameObject, "SetupCustomLinks");
                }
                
                var p0 = groupA.stars![customLink.a.y].position;
                var p1 = groupB.stars![customLink.b.y].position;
                
                s.SetPosition(0, p0 + (p1 - p0).normalized * _linkOffset);
                s.SetPosition(1, p1 + (p0 - p1).normalized * _linkOffset);

                s.widthMultiplier = _linkWidth;
                
                changed = true;
            }

            if (changed) {
                UnityEditor.EditorUtility.SetDirty(gameObject);
            }
        }

        private void OnValidate() {
            if (!_enableEditMode) return;
            
            SetupStarPrefabs();
            SelectDefaultStarGroup();
            SetupCustomLinks();
        }
#endif
    }
    
}