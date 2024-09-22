using System;
using MisterGames.Actors;
using MisterGames.Character.View;
using MisterGames.Common.Data;
using MisterGames.Common.Lists;
using MisterGames.Common.Maths;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

namespace _Project.Scripts.Runtime.Fireball {
    
    public sealed class FireballShaderController : MonoBehaviour, IActorComponent, IUpdate {

        [Header("Material")]
        [SerializeField] private CustomPassVolume _customPassVolume;
        [SerializeField] private Material _material;
        [SerializeField] private string _centerOffsetProperty;
        [SerializeField] private string _distortionBlendProperty;
        [SerializeField] private string _colorBlendProperty;
        [SerializeField] private string _colorDistortionBlendProperty;
        [SerializeField] private string _colorVignetteProperty;
        [SerializeField] private string _colorBorderProperty;

        [Header("Center Offset")]
        [SerializeField] [Min(1)] private int _bufferSize = 5;
        [SerializeField] private float _minSpeed = 0.1f;
        [SerializeField] private float _maxSpeed = 1f;
        [SerializeField] private float _speedMul = 1f;
        [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] [Min(0f)] private float _maxOffset;
        [SerializeField] [Min(0f)] private float _offsetSmoothing = 5f;

        [Header("Stages")]
        [SerializeField] [Min(0f)] private float _defaultSmoothing = 5f;
        [SerializeField] private StageSetting[] _stageSettings;
        
        [Serializable]
        private struct StageSetting {
            public Optional<FireballBehaviour.Stage> previousStage;
            public FireballBehaviour.Stage currentStage;
            [Min(0f)] public float smoothingStart;
            [Min(0f)] public float smoothingEnd;
            [Range(0f, 1f)] public float distortionBlendStart;
            [Range(0f, 1f)] public float distortionBlendEnd;
            [Range(0f, 1f)] public float colorBlendStart;
            [Range(0f, 1f)] public float colorBlendEnd;
            [Range(0f, 1f)] public float colorDistortionBlendStart;
            [Range(0f, 1f)] public float colorDistortionBlendEnd;
            [Range(0f, 1f)] public float colorVignetteStart;
            [Range(0f, 1f)] public float colorVignetteEnd;
            [Range(0f, 1f)] public float colorBorderStart;
            [Range(0f, 1f)] public float colorBorderEnd;
            [FormerlySerializedAs("distortionCurve")] public AnimationCurve blendCurve;

            public static readonly StageSetting Default = new() {
                blendCurve = AnimationCurve.Constant(0f, 1f, 0f),
            };
        }
        
        private FireballBehaviour _fireballBehaviour;
        private CharacterViewPipeline _view;

        private Material _runtimeMaterial;
        private int _centerOffsetId;
        private int _distortionBlendId;
        private int _colorBlendId;
        private int _colorDistortionBlendId;
        private int _colorVignetteId;
        private int _colorBorderId;

        private Vector2[] _deltaBuffer;
        private float[] _speedBuffer;
        private int _speedPointer;
        private int _deltaPointer;
        private Vector3 _lastPoint;
        private Vector2 _centerOffsetSmoothed;

        private StageSetting _currentStageSetting;
        private float _distortionBlend;
        private float _colorBlend;
        private float _colorDistortionBlend;
        private float _colorVignette;
        private float _colorBorder;

        void IActorComponent.OnAwake(IActor actor) {
            _view = actor.GetComponent<CharacterViewPipeline>();
            _fireballBehaviour = actor.GetComponent<FireballBehaviour>();
            
            _speedBuffer = new float[_bufferSize];
            _deltaBuffer = new Vector2[_bufferSize];
            
            InstantiateMaterial();
        }

        private void OnEnable() {
            ResetCenterOffset();
            ResetBlends();
            
            OnStageChanged(_fireballBehaviour.CurrentStage, _fireballBehaviour.CurrentStage);
            
            _fireballBehaviour.OnStageChanged += OnStageChanged;
            PlayerLoopStage.LateUpdate.Subscribe(this);
        }

        private void OnDisable() {
            _fireballBehaviour.OnStageChanged -= OnStageChanged;
            PlayerLoopStage.LateUpdate.Unsubscribe(this);
        }

        private void InstantiateMaterial() {
            _runtimeMaterial = new Material(_material);
            ((FullScreenCustomPass) _customPassVolume.customPasses[0]).fullscreenPassMaterial = _runtimeMaterial;
            
            _centerOffsetId = Shader.PropertyToID(_centerOffsetProperty);
            _distortionBlendId = Shader.PropertyToID(_distortionBlendProperty);
            _colorBlendId = Shader.PropertyToID(_colorBlendProperty);
            _colorDistortionBlendId = Shader.PropertyToID(_colorDistortionBlendProperty);
            _colorVignetteId = Shader.PropertyToID(_colorVignetteProperty);
            _colorBorderId = Shader.PropertyToID(_colorBorderProperty);
        }

        private void ResetCenterOffset() {
            _lastPoint = _view.Orientation * Vector3.forward;
            _centerOffsetSmoothed = Vector2.zero;
        }

        private void ResetBlends() {
            _currentStageSetting = GetDefaultStageSetting();
            
            _distortionBlend = 0f;
            _colorBlend = 0f;
            _colorDistortionBlend = 0f;
            _colorVignette = 0f;
            _colorBorder = 0f;
        }

        private void OnStageChanged(FireballBehaviour.Stage previous, FireballBehaviour.Stage current) {
            for (int i = 0; i < _stageSettings.Length; i++) {
                ref var setting = ref _stageSettings[i];
                
                if (setting.currentStage != current ||
                    setting.previousStage.HasValue && setting.previousStage.Value != previous
                ) {
                    continue;
                }

                _currentStageSetting = setting;
                return;
            }

            _currentStageSetting = GetDefaultStageSetting();
        }

        void IUpdate.OnUpdate(float dt) {
            ProcessCenterOffset(dt);
            ProcessCurrentStage(dt);
        }

        private void ProcessCenterOffset(float dt) {
            var orient = _view.Orientation;
            var point = orient * Vector3.forward;
            var delta = Quaternion.Inverse(orient) * (point - _lastPoint);
            _lastPoint = point;

            float speed = dt > 0f ? delta.magnitude / dt : 0f;
            float speedAvg = _speedBuffer.WriteToCircularBufferAndGetAverage(speed, ref _speedPointer);
            float relativeSpeed = Mathf.Clamp01(Mathf.Max(speedAvg - _minSpeed, 0f) / (_maxSpeed - _minSpeed));

            var deltaAvg = _deltaBuffer.WriteToCircularBufferAndGetAverage(delta, ref _deltaPointer);
            var centerOffset = deltaAvg.normalized * Mathf.Min(_speedCurve.Evaluate(relativeSpeed) * _speedMul, _maxOffset);
            _centerOffsetSmoothed = _centerOffsetSmoothed.SmoothExp(centerOffset, dt * _offsetSmoothing);
            
            _runtimeMaterial.SetVector(_centerOffsetId, _centerOffsetSmoothed);
        }

        private void ProcessCurrentStage(float dt) {
            float p = _fireballBehaviour.StageProgress;
            var curve = _currentStageSetting.blendCurve;
            
            float distortionBlend = _currentStageSetting.distortionBlendStart + 
                                    curve.Evaluate(p) * (_currentStageSetting.distortionBlendEnd - _currentStageSetting.distortionBlendStart);
            float colorBlend = _currentStageSetting.colorBlendStart + 
                               curve.Evaluate(p) * (_currentStageSetting.colorBlendEnd - _currentStageSetting.colorBlendStart);
            float colorDistortionBlend = _currentStageSetting.colorDistortionBlendStart + 
                                         curve.Evaluate(p) * (_currentStageSetting.colorDistortionBlendEnd - _currentStageSetting.colorDistortionBlendStart);
            float colorVignette = _currentStageSetting.colorVignetteStart + 
                                  curve.Evaluate(p) * (_currentStageSetting.colorVignetteEnd - _currentStageSetting.colorVignetteStart);
            float colorBorder = _currentStageSetting.colorBorderStart + 
                                curve.Evaluate(p) * (_currentStageSetting.colorBorderEnd - _currentStageSetting.colorBorderStart);
            
            float smoothing = _currentStageSetting.smoothingStart + 
                              curve.Evaluate(p) * (_currentStageSetting.smoothingEnd - _currentStageSetting.smoothingStart);
            
            _distortionBlend = _distortionBlend.SmoothExp(distortionBlend, dt * smoothing);
            _colorBlend = _colorBlend.SmoothExp(colorBlend, dt * smoothing);
            _colorDistortionBlend = _colorDistortionBlend.SmoothExp(colorDistortionBlend, dt * smoothing);
            _colorVignette = _colorVignette.SmoothExp(colorVignette, dt * smoothing);
            _colorBorder = _colorBorder.SmoothExp(colorBorder, dt * smoothing);

            _runtimeMaterial.SetFloat(_distortionBlendId, _distortionBlend);
            _runtimeMaterial.SetFloat(_colorBlendId, _colorBlend);
            _runtimeMaterial.SetFloat(_colorDistortionBlendId, _colorDistortionBlend);
            _runtimeMaterial.SetFloat(_colorVignetteId, _colorVignette);
            _runtimeMaterial.SetFloat(_colorBorderId, _colorBorder);
        }
        
        private StageSetting GetDefaultStageSetting() {
            var setting = StageSetting.Default;
            setting.smoothingStart = _defaultSmoothing;
            setting.smoothingEnd = _defaultSmoothing;
            return setting;
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (Application.isPlaying) {
                if (_speedBuffer?.Length != _bufferSize) {
                    _speedBuffer = new float[_bufferSize];
                    _speedPointer = 0;
                }

                if (_deltaBuffer?.Length != _bufferSize) {
                    _deltaBuffer = new Vector2[_bufferSize];
                    _deltaPointer = 0;
                }
            }
        }
#endif
    }
    
}