using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Character.View;
using MisterGames.Common.Async;
using MisterGames.Common.Pooling;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using MisterGames.Common.Tick;
using MisterGames.Tweens;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Telescope {
    
    public sealed class StarGroupDetection : MonoBehaviour, IUpdate {
        
        [SerializeField] private StarGroupsData _starGroupsData;
        [SerializeField] private Interactive _interactive;
        [SerializeField] private StarGroupsPlacer _starGroupsPlacer;
        
        [Header("Lens")]
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _lensPlace;
        [SerializeField] private Vector3 _lensOffset;
        [SerializeField] private Vector3 _lensRotationOffset;
        
        [Header("Events")]
        [SerializeField] private EventReference _pickLensEvent;
        [SerializeField] private EventReference _detectStarGroupEvent;
        
        private static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");
        
        private readonly Dictionary<Transform, Renderer> _rendererMap = new();
        private CancellationTokenSource _enableCts;
        private float[] _detectionTimers;
        
        private GameObject _currentLens;
        private CharacterViewPipeline _view;
        private int _selectedStarGroupIndex = -1;
        private byte _setupLensId;
        
        private void Awake() {
            FetchInitialData();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            
            _interactive.OnStartInteract += OnStartInteract;
            _interactive.OnStopInteract += OnStopInteract;
            
            if (_interactive.IsInteracting) PlayerLoopStage.Update.Subscribe(this);
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            
            _interactive.OnStartInteract -= OnStartInteract;
            _interactive.OnStopInteract -= OnStopInteract;
            
            PlayerLoopStage.Update.Unsubscribe(this);
        }

        private void FetchInitialData() {
            _rendererMap.Clear();
            _detectionTimers = new float[_starGroupsPlacer.StarGroups.Count];
            
            for (int i = 0; i < _starGroupsPlacer.StarGroups.Count; i++) {
                var starGroup = _starGroupsPlacer.StarGroups[i];
                
                for (int j = 0; j < starGroup.stars.Count; j++) {
                    var star = starGroup.stars[j];
                    var renderer = star.GetComponent<Renderer>();

                    if (renderer.material == renderer.sharedMaterial) {
                        renderer.material = new Material(renderer.material);
                    }
                    
                    _rendererMap.Add(star, renderer);
                }
            }
        }

        private void OnStartInteract(IInteractiveUser user) {
            _view = user.Root.GetComponent<IActor>().GetComponent<CharacterViewPipeline>();
            PlayerLoopStage.Update.Subscribe(this);
            
            SetupLens(delay: 0f, _enableCts.Token).Forget();
        }

        private void OnStopInteract(IInteractiveUser user) {
            PlayerLoopStage.Update.Unsubscribe(this);

            for (int i = 0; i < _detectionTimers.Length; i++) {
                ref float t = ref _detectionTimers[i];
                if (t < _starGroupsData.detectionTime) t = 0f;
            }
        }

        private async UniTask SetupLens(float delay, CancellationToken cancellationToken) {
            byte id = ++_setupLensId;
            
            if (delay > 0f) {
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();   
            }
            
            if (cancellationToken.IsCancellationRequested || id != _setupLensId) return;
            
            if (_currentLens != null) {
                var canvas = _currentLens.GetComponentInChildren<StarGroupsCanvas>(includeInactive: true);
                if (_detectStarGroupEvent.WithSubId(canvas.SelectedStarGroupIndex).GetCount() <= 0) return;

                _selectedStarGroupIndex = -1;
                await TakeLensOff(cancellationToken);
            }
            
            if (cancellationToken.IsCancellationRequested || id != _setupLensId) return;
            
            int count = _starGroupsPlacer.StarGroups.Count;
            int selectedIndex = -1;
            
            for (int i = 0; i < count; i++) {
                if (_pickLensEvent.WithSubId(i).GetCount() <= 0 ||
                    _detectStarGroupEvent.WithSubId(i).GetCount() > 0
                ) {
                    continue;
                }

                selectedIndex = i;
                break;
            }

            _selectedStarGroupIndex = selectedIndex;
            
            if (selectedIndex < 0) return;
            
            var lens = CreateLens(selectedIndex);
            await TakeLensOn(lens, cancellationToken);
        }
        
        private async UniTask TakeLensOn(GameObject lens, CancellationToken cancellationToken) {
            if (_currentLens != null) PrefabPool.Main.Release(_currentLens);
            _currentLens = lens;

            var t = _currentLens.transform;
            var lensPlaceRot = _lensPlace.rotation;
            
            t.SetPositionAndRotation(
                _lensPlace.position + lensPlaceRot * _lensOffset,
                lensPlaceRot * Quaternion.Euler(_lensRotationOffset)
            );
            
            t.parent = _lensPlace;
            
            _currentLens.SetActive(true);

            var collider = _currentLens.GetComponentInChildren<Collider>();
            collider.enabled = false;
            
            var tweenPlayer = _currentLens.GetComponent<TweenRunner>().TweenPlayer;
            tweenPlayer.Progress = 1f;
            tweenPlayer.Speed = -1f;
            
            await tweenPlayer.Play(cancellationToken: cancellationToken);
        }
        
        private async UniTask TakeLensOff(CancellationToken cancellationToken) {
            var lens = _currentLens;
            _currentLens = null;
            
            if (lens == null) return;
            
            var tweenPlayer = lens.GetComponent<TweenRunner>().TweenPlayer;
            tweenPlayer.Progress = 0f;
            tweenPlayer.Speed = 1f;
            
            await tweenPlayer.Play(cancellationToken: cancellationToken);
            
            PrefabPool.Main.Release(lens);
        }

        private GameObject CreateLens(int groupIndex) {
            var prefab = _starGroupsData.starGroups[groupIndex].lensPrefab;
            var instance = PrefabPool.Main.Get(prefab.gameObject, active: false);
            return instance;
        }

        void IUpdate.OnUpdate(float dt) {
            for (int i = 0; i < _starGroupsPlacer.StarGroups.Count; i++) {
                var starGroup = _starGroupsPlacer.StarGroups[i];
                var center = Vector3.zero;
                ref float detectionTimer = ref _detectionTimers[i];
                
                for (int j = 0; j < starGroup.stars.Count; j++) {
                    center += starGroup.stars[j].position;
                }

                center /= starGroup.stars.Count > 0 ? starGroup.stars.Count : 1;

                float angle = GetAngleToPoint(center);
                
                bool inHoverAngle = angle <= _starGroupsData.hoverAngle;
                bool inDetectionAngle = angle <= _starGroupsData.detectionAngle;
                bool isFovInRange = IsFovInRange(_camera.fieldOfView, _starGroupsData.detectionFovRange);

                float hoverCoeff = inHoverAngle ? angle / _starGroupsData.hoverAngle : 0f;
                bool wasLessThanDetectionTime = false;
                
                if (detectionTimer < _starGroupsData.detectionTime) {
                    wasLessThanDetectionTime = true;
                    detectionTimer = inDetectionAngle && isFovInRange && i == _selectedStarGroupIndex 
                        ? detectionTimer + dt 
                        : 0f;   
                }
                
                if (i == _selectedStarGroupIndex &&
                    inDetectionAngle && isFovInRange && 
                    wasLessThanDetectionTime &&
                    detectionTimer >= _starGroupsData.detectionTime) 
                {
                    _detectStarGroupEvent.WithSubId(i).SetCount(1);
                }
                
                if (i == _selectedStarGroupIndex &&
                    wasLessThanDetectionTime &&
                    detectionTimer >= _starGroupsData.detectionTime) 
                {
                    SetupLens(_starGroupsData.takeLensOffDelayAfterDetection, _enableCts.Token).Forget();
                }
                
                for (int j = 0; j < starGroup.stars.Count; j++) {
                    var star = starGroup.stars[j];
                    var renderer = _rendererMap[star];

                    var targetColor = detectionTimer >= _starGroupsData.detectionTime
                        ? ApplyModulation(
                            _starGroupsData.emissionDetected,
                            _starGroupsData.emissionFrequencyDetected,
                            _starGroupsData.emissionRangeDetected,
                            star.position.sqrMagnitude
                        )
                        : inHoverAngle
                            ? ApplyModulation(
                                _starGroupsData.emissionHover,
                                Mathf.Lerp(_starGroupsData.emissionFrequencyHoverMax, _starGroupsData.emissionFrequencyHover, hoverCoeff),
                                Mathf.Lerp(_starGroupsData.emissionRangeHoverMax, _starGroupsData.emissionRangeHover, hoverCoeff),
                                star.position.sqrMagnitude
                            )
                            : _starGroupsData.emissionNormal;
                        
                    var color = Color.Lerp(renderer.material.GetColor(EmissiveColor), targetColor, _starGroupsData.emissionSmoothing * dt);
                    renderer.material.SetColor(EmissiveColor, color);
                }
            }
        }

        private Color ApplyModulation(Color inputColor, float frequency, float range, float offset) {
            return inputColor * (1f + Mathf.Sin(frequency * Time.time + Random.Range(-offset, offset)) * range);
        }

        private float GetAngleToPoint(Vector3 point) {
            return Vector3.Angle(point - _starGroupsPlacer.Center.position, _view.HeadRotation * Vector3.forward);
        }

        private bool IsFovInRange(float fov, Vector2 fovRange) {
            return fov >= fovRange.x && fov <= fovRange.y;
        }
    }
    
}