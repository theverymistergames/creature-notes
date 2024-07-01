using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.GameObjects;
using MisterGames.Common.Maths;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Runtime.Telescope {
    
    public sealed class StarsMainPainting : MonoBehaviour {
        
        [SerializeField] private Interactive _interactive;
        [SerializeField] private StarGroupsCanvas _starGroupsCanvas;
        [SerializeField] private EventReference _starGroupDetectedEvent;
        [SerializeField] private EventReference _lensFoundEvent;
        [SerializeField] private EventReference _starGroupPaintedEvent;

        [Header("Fade")]
        [SerializeField] [Min(0f)] private float _fadeDuration = 3f;
        [SerializeField] [Min(0f)] private float _fadeDurationFinal = 3f;
        [SerializeField] [Min(0f)] private float _fadeSmoothingFinal = 3f;
        [SerializeField] private AnimationCurve _fadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private float _fadeNoise = 0.2f;
        [SerializeField] private float _fadeNoiseFreq = 1f;
        
        [Header("Color")]
        [ColorUsage(showAlpha: true)]
        [SerializeField] private Color _starColor;
        [SerializeField] private float _lineEmission = 1f;
        [SerializeField] private float _starEmission = 1f;
        [SerializeField] private float _lineEmissionFinal = 1f;

        [Header("Groups")]
        [SerializeField] private bool _allDetected;
        [SerializeField] private GroupData[] _groups;

        [Header("Actions")]
        [SerializeReference] [SubclassSelector] private IActorAction _onInteractFirst;
        [SerializeReference] [SubclassSelector] private IActorAction _onInteractDetectedLessThanFound;
        [SerializeReference] [SubclassSelector] private IActorAction _onInteractDetectedEqualsFound;
        [SerializeReference] [SubclassSelector] private IActorAction _onInteractDetectedAll;

        private static readonly int _EmissiveColor = Shader.PropertyToID("_EmissiveColor");
        private static readonly int _Color = Shader.PropertyToID("_UnlitColor");
        
        private CancellationTokenSource _enableCts;
        private byte _checkId;
        private bool _hasFirstInteract;
        
        [Serializable]
        private struct GroupData {
            public bool detected;
            [NonSerialized] public float fader;
            public Image[] starImages;
            public Renderer[] lines;
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            _interactive.OnStartInteract += OnStartInteract;
            
            CheckStarGroups(_allDetected, force: true);
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            _interactive.OnStartInteract -= OnStartInteract;
        }

        private void FetchGroupElements() {
            if (_groups == null) _groups = new GroupData[_starGroupsCanvas.StarGroupCount];
            else Array.Resize(ref _groups, _starGroupsCanvas.StarGroupCount);

            for (int i = 0; i < _groups.Length; i++) {
                ref var groupData = ref _groups[i];
                
                var lines = _starGroupsCanvas.GetGroupLines(i);
                var stars = _starGroupsCanvas.GetGroupStars(i);

                if (groupData.starImages == null) groupData.starImages = new Image[stars.Count];
                else Array.Resize(ref groupData.starImages, stars.Count);
                
                if (groupData.lines == null) groupData.lines = new Renderer[lines.Count];
                else Array.Resize(ref groupData.lines, lines.Count);
                
                for (int j = 0; j < groupData.starImages.Length; j++) {
                    var s = stars[j];
                    if (s == null) continue;
                    
                    ref var image = ref groupData.starImages[j];
                    image = s.GetComponent<Image>();
                }
                
                for (int j = 0; j < lines.Count; j++) {
                    var l = lines[j];
                    if (l == null) continue;
                    
                    groupData.lines[j] = l.GetComponent<Renderer>();
                    
#if UNITY_EDITOR
                    if (!Application.isPlaying) continue;
#endif
                    
                    groupData.lines[j].SetupUniqueMaterial();
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            
            var customLines = _starGroupsCanvas.GetCustomLines();
            for (int i = 0; i < customLines.Count; i++) {
                customLines[i].SetupUniqueMaterial();
            }
        }

        private void OnStartInteract(IInteractiveUser user) {
            if (_allDetected) return;
            
            CheckEvents(out bool allDetected, out int partsRevealed, out int detectedCount, out int foundCount);

            _allDetected = allDetected;
            
            CheckActions(user, allDetected, partsRevealed, detectedCount, foundCount);
            CheckStarGroups(allDetected);
        }

        private void CheckActions(IInteractiveUser user, bool allDetected, int partsRevealed, int detectedCount, int foundCount) {
            var actor = user.Root.GetComponent<IActor>();

            if (!_hasFirstInteract) {
                _hasFirstInteract = true;
                _onInteractFirst?.Apply(actor, _enableCts.Token).Forget();
                if (!allDetected) return;
            }
            
            if (allDetected) {
                _onInteractDetectedAll?.Apply(actor, _enableCts.Token).Forget();
                return;
            }
            
            // Skip actions to reveal star groups
            if (partsRevealed < detectedCount) return;
            
            if (detectedCount < foundCount) {
                _onInteractDetectedLessThanFound?.Apply(actor, _enableCts.Token).Forget();
                return;
            }
            
            _onInteractDetectedEqualsFound?.Apply(actor, _enableCts.Token).Forget();
        }

        private void CheckEvents(out bool allDetected, out int partsRevealed, out int detectedCount, out int foundCount) {
            int groupCount = _starGroupsCanvas.StarGroupCount;

            partsRevealed = 0;
            foundCount = 0;
            detectedCount = 0;

            for (int i = 0; i < groupCount; i++) {
                partsRevealed += _groups[i].detected.AsInt();
                foundCount += _lensFoundEvent.WithSubId(i).IsRaised().AsInt();
                detectedCount += _starGroupDetectedEvent.WithSubId(i).IsRaised().AsInt();
            }

            allDetected = detectedCount >= groupCount;
        }

        private void CheckStarGroups(bool allDetected, bool force = false) {
            byte id = ++_checkId;
            int totalGroupsCount = _groups?.Length ?? 0;

            for (int i = 0; i < totalGroupsCount; i++) {
                ref var group = ref _groups[i];
                
                bool groupDetected = _starGroupDetectedEvent.WithSubId(i).GetRaiseCount() > 0;
                if (!force && groupDetected == group.detected && !allDetected) continue;

                bool wasDetected = group.detected;
                
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    FadeGroup(id, i, force ? 0f : _fadeDuration, wasDetected, allDetected, _enableCts?.Token ?? default).Forget();    
                    continue;
                }
#endif

                group.detected = groupDetected;
                _starGroupPaintedEvent.WithSubId(i).SetCount(groupDetected.AsInt());

                FadeGroup(id, i, force ? 0f : _fadeDuration, wasDetected, allDetected, _enableCts?.Token ?? default).Forget();
            }
        }

        private async UniTask FadeGroup(byte id, int index, float duration, bool wasDetected, bool allDetected, CancellationToken cancellationToken) {
            if (index < 0 || index >= (_groups?.Length ?? 0)) return;

            float speed = duration > 0f ? 1f / duration : float.MaxValue;
            float t = 0f;
            var ts = PlayerLoopStage.Update.Get();

            while (!cancellationToken.IsCancellationRequested && _checkId == id) {
                t = Mathf.Clamp(t + speed * ts.DeltaTime, 0f, 1f + _fadeNoise);
                
                if (!wasDetected) UpdateGroup(index, _fadeCurve.Evaluate(t), false, ts.DeltaTime);

                if (t >= 1f + _fadeNoise) break;

                await UniTask.Yield();
            }
            
            if (!allDetected) return;
            
            speed = _fadeDurationFinal > 0f ? 1f / _fadeDurationFinal : float.MaxValue;
            t = 0f;
            
            while (!cancellationToken.IsCancellationRequested && _checkId == id) {
                t = Mathf.Clamp(t + speed * ts.DeltaTime, 0f, 1f + _fadeNoise);
                UpdateGroup(index, _fadeCurve.Evaluate(t), true, ts.DeltaTime);

                if (t >= 1f + _fadeNoise) break;

                await UniTask.Yield();
            }
        }

        private void UpdateGroup(int index, float t, bool allDetected, float dt) { 
            ref var group = ref _groups[index];
            
            float startValue = (!group.detected).AsFloat();
            float endValue = group.detected.AsFloat();
            var starImages = group.starImages;
            var lines = group.lines;
            
            var customLines = _starGroupsCanvas.GetCustomLines();
            var customLinks = _starGroupsCanvas.CustomLinks;

            var startColorStar = allDetected ? _starColor * _starEmission : startValue * _starEmission * _starColor;
            var endColorStar = allDetected ? _starColor * _starEmission : endValue * _starEmission * _starColor;

            var startColorLine = allDetected ? _starColor : startValue * _starColor;
            var endColorLine = allDetected ? _starColor : endValue * _starColor;
            
            var startColorLineEmissive = startColorLine * _lineEmission;
            var endColorLineEmissive = allDetected ? endColorLine * _lineEmissionFinal : endColorLine * _lineEmission;
            
            _groups[index].fader = Mathf.Clamp01(Mathf.Lerp(startValue, endValue, t));
                
            for (int i = 0; i < starImages.Length; i++) {
                var starImage = starImages[i];
                if (starImage == null) continue;
                    
                float p = t + _fadeNoise * Mathf.PerlinNoise1D(Time.time * _fadeNoiseFreq + (float) i / starImages.Length);
                starImage.color = Color.Lerp(startColorStar, endColorStar, p);
            }

            for (int i = 0; i < lines.Length; i++) {
                var line = lines[i];
                if (line == null) continue;
                    
                float p = t + _fadeNoise * Mathf.PerlinNoise1D(Time.time * _fadeNoiseFreq + (float) i / lines.Length);

#if UNITY_EDITOR
                var mat = Application.isPlaying ? line.material : line.sharedMaterial; 
                if (mat == null) continue;
#else
                var mat = line.material;
#endif

                if (allDetected) {
                    mat.SetColor(
                        _EmissiveColor,
                        Color.Lerp(mat.GetColor(_EmissiveColor), Color.Lerp(startColorLineEmissive, endColorLineEmissive, p), dt * _fadeSmoothingFinal)
                    );
                }
                else {
                    mat.SetColor(_EmissiveColor, Color.Lerp(startColorLineEmissive, endColorLineEmissive, p));   
                }
                
                mat.SetColor(_Color, Color.Lerp(startColorLine, endColorLine, p));
            }

            for (int i = 0; i < customLinks.Count; i++) {
                var customLink = customLinks[i];
                var line = customLines[i];
                    
                if (index != customLink.a.x && index != customLink.b.x || line == null) continue;

                bool detectedA = _groups[customLink.a.x].detected;
                bool detectedB = _groups[customLink.b.x].detected;

                bool enableLine = detectedA && detectedB;
                float p = t + _fadeNoise * Mathf.PerlinNoise1D(Time.time * _fadeNoiseFreq + (float) i / customLinks.Count);
                float v0 = (!enableLine).AsFloat() * _groups[customLink.a.x].fader * _groups[customLink.b.x].fader;
                float v1 = enableLine.AsFloat();

                var startColor = allDetected ? startColorLine : v0 * startColorLine;
                var endColor = allDetected ? endColorLine : v1 * endColorLine;
                
                var startColorEmissive = allDetected ? startColorLineEmissive : v0 * startColorLineEmissive;
                var endColorEmissive = allDetected ? endColorLineEmissive : v1 * endColorLineEmissive;
                    
#if UNITY_EDITOR
                var mat = Application.isPlaying ? line.material : line.sharedMaterial; 
                if (mat == null) continue;
#else
                var mat = line.material;
#endif
                
                mat.SetColor(_Color, Color.Lerp(startColor, endColor, p));
                
                if (allDetected) {
                    mat.SetColor(
                        _EmissiveColor,
                        Color.Lerp(mat.GetColor(_EmissiveColor), Color.Lerp(startColorEmissive, v1 * endColorEmissive, p), dt * _fadeSmoothingFinal)
                    );
                }
                else {
                    mat.SetColor(_EmissiveColor, Color.Lerp(startColorEmissive, endColorEmissive, p));   
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (_starGroupsCanvas == null) return;
            
            int groups = _starGroupsCanvas.StarGroupCount;
            for (int i = 0; i < groups; i++) {
                var stars = _starGroupsCanvas.GetGroupStars(i);
                var lines = _starGroupsCanvas.GetGroupLines(i);
                
                for (int j = 0; j < stars.Count; j++) {
                    var star = stars[j];
                    if (star == null) continue;
                    
                    if (star.gameObject.TryGetComponent(out Image image)) {
                        UnityEditor.Undo.RecordObject(image, "StarsColorValidate");
                    }
                }

                for (int j = 0; j < lines.Count; j++) {
                    var line = lines[j];
                    if (line == null) continue;
                    
                    var r = line.GetComponent<Renderer>();
                    if (r == null || r.sharedMaterial == null) continue;
                    
                    UnityEditor.Undo.RecordObject(r, "StarsColorValidate");
                    UnityEditor.Undo.RecordObject(r.sharedMaterial, "StarsColorValidate");
                }
            }

            var customLines = _starGroupsCanvas.GetCustomLines();
            for (int i = 0; i < customLines.Count; i++) {
                var line = customLines[i];
                if (line == null) continue;
                    
                var r = line.GetComponent<Renderer>();
                if (r == null || r.sharedMaterial == null) continue;
                
                UnityEditor.Undo.RecordObject(r, "StarsColorValidate");
                UnityEditor.Undo.RecordObject(r.sharedMaterial, "StarsColorValidate");
            }
            
            FetchGroupElements();
            CheckStarGroups(_allDetected, force: true);
            
            for (int i = 0; i < groups; i++) {
                var stars = _starGroupsCanvas.GetGroupStars(i);
                var lines = _starGroupsCanvas.GetGroupLines(i);
                
                for (int j = 0; j < stars.Count; j++) {
                    var star = stars[j];
                    if (star == null) continue;
                    
                    if (star.gameObject.TryGetComponent(out Image image)) {
                        UnityEditor.EditorUtility.SetDirty(image);
                    }
                }

                for (int j = 0; j < lines.Count; j++) {
                    var line = lines[j];
                    if (line == null) continue;
                    
                    var r = line.GetComponent<Renderer>();
                    if (r == null || r.sharedMaterial == null) continue;
                    
                    UnityEditor.EditorUtility.SetDirty(r);
                    UnityEditor.EditorUtility.SetDirty(r.sharedMaterial);
                }
            }

            for (int i = 0; i < customLines.Count; i++) {
                var line = customLines[i];
                if (line == null) continue;
                    
                var r = line.GetComponent<Renderer>();
                if (r == null || r.sharedMaterial == null) continue;
                
                UnityEditor.EditorUtility.SetDirty(r);
                UnityEditor.EditorUtility.SetDirty(r.sharedMaterial);
            }
            
            UnityEditor.EditorUtility.SetDirty(gameObject);
        }
#endif
    }
    
}