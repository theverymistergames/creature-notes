using System;
using MisterGames.Actors;
using MisterGames.Character.View;
using MisterGames.Common.Data;
using MisterGames.Common.Lists;
using MisterGames.Common.Maths;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

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
        [SerializeField] private StageSetting[] _stages;

        [Header("Fire")]
        [SerializeField] [Min(0f)] private float _fireDurationMin;
        [SerializeField] [Min(0f)] private float _fireDurationMax;
        [SerializeField] [Range(0f, 1f)] private float _startFireProgressMax;
        [SerializeField] private MaterialSetting _fireMaterialSetting;
        
        [Serializable]
        private struct StageSetting {
            public Optional<FireballBehaviour.Stage> previousStage;
            public FireballBehaviour.Stage currentStage;
            public MaterialSetting setting;
        }

        [Serializable]
        private sealed class MaterialSetting {
            public AnimationCurve blendCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
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

        private MaterialSetting _defaultSetting;
        private MaterialSetting _currentSetting;
        private float _distortionBlend;
        private float _colorBlend;
        private float _colorDistortionBlend;
        private float _colorVignette;
        private float _colorBorder;

        private float _fireProgress;
        private float _fireSpeed;

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
            
            _fireballBehaviour.OnStageChanged += OnStageChanged;
            _fireballBehaviour.OnFire += OnFire;
            
            PlayerLoopStage.LateUpdate.Subscribe(this);
        }

        private void OnDisable() {
            _fireballBehaviour.OnStageChanged -= OnStageChanged;
            _fireballBehaviour.OnFire -= OnFire;
            
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
            
            _defaultSetting = new MaterialSetting {
                blendCurve = AnimationCurve.Constant(0f, 0f, 0f),
                smoothingStart = _defaultSmoothing,
                smoothingEnd = _defaultSmoothing,
            };
        }

        private void ResetCenterOffset() {
            _lastPoint = _view.Orientation * Vector3.forward;
            _centerOffsetSmoothed = Vector2.zero;
        }

        private void ResetBlends() {
            _currentSetting = _defaultSetting;
            
            _distortionBlend = 0f;
            _colorBlend = 0f;
            _colorDistortionBlend = 0f;
            _colorVignette = 0f;
            _colorBorder = 0f;

            _fireProgress = 1f;
            _fireSpeed = 0f;
            
            ApplyMaterialProperties();
        }

        private void OnFire(float progress) {
            float duration = _fireDurationMin + progress * (_fireDurationMax - _fireDurationMin);
            _fireProgress = Mathf.Min(_startFireProgressMax, 1f - progress);
            _fireSpeed = duration > 0f ? 1f / duration : float.MaxValue;
        }

        private void OnStageChanged(FireballBehaviour.Stage previous, FireballBehaviour.Stage current) {
            for (int i = 0; i < _stages.Length; i++) {
                ref var stage = ref _stages[i];
                
                if (stage.currentStage != current ||
                    stage.previousStage.HasValue && stage.previousStage.Value != previous
                ) {
                    continue;
                }

                _currentSetting = stage.setting;
                return;
            }

            _currentSetting = _defaultSetting;
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
            _fireProgress = Mathf.Clamp01(_fireProgress + dt * _fireSpeed);
            
            MaterialSetting setting;
            float progress;

            if (_fireProgress < 1f) {
                setting = _fireMaterialSetting;
                progress = _fireProgress;
            }
            else {
                setting = _currentSetting;
                progress = _fireballBehaviour.StageProgress;
            }
            
            ProcessMaterialSetting(setting, progress, dt);
        }

        private void ProcessMaterialSetting(MaterialSetting setting, float progress, float dt) {
            float distortionBlend = setting.distortionBlendStart + setting.blendCurve.Evaluate(progress) * (setting.distortionBlendEnd - setting.distortionBlendStart);
            float colorBlend = setting.colorBlendStart + setting.blendCurve.Evaluate(progress) * (setting.colorBlendEnd - setting.colorBlendStart);
            float colorDistortionBlend = setting.colorDistortionBlendStart + setting.blendCurve.Evaluate(progress) * (setting.colorDistortionBlendEnd - setting.colorDistortionBlendStart);
            float colorVignette = setting.colorVignetteStart + setting.blendCurve.Evaluate(progress) * (setting.colorVignetteEnd - setting.colorVignetteStart);
            float colorBorder = setting.colorBorderStart + setting.blendCurve.Evaluate(progress) * (setting.colorBorderEnd - setting.colorBorderStart);
            
            float smoothing = setting.smoothingStart + setting.blendCurve.Evaluate(progress) * (setting.smoothingEnd - setting.smoothingStart);
            
            _distortionBlend = _distortionBlend.SmoothExp(distortionBlend, dt * smoothing);
            _colorBlend = _colorBlend.SmoothExp(colorBlend, dt * smoothing);
            _colorDistortionBlend = _colorDistortionBlend.SmoothExp(colorDistortionBlend, dt * smoothing);
            _colorVignette = _colorVignette.SmoothExp(colorVignette, dt * smoothing);
            _colorBorder = _colorBorder.SmoothExp(colorBorder, dt * smoothing);

            ApplyMaterialProperties();
        }

        private void ApplyMaterialProperties() {
            _runtimeMaterial.SetFloat(_distortionBlendId, _distortionBlend);
            _runtimeMaterial.SetFloat(_colorBlendId, _colorBlend);
            _runtimeMaterial.SetFloat(_colorDistortionBlendId, _colorDistortionBlend);
            _runtimeMaterial.SetFloat(_colorVignetteId, _colorVignette);
            _runtimeMaterial.SetFloat(_colorBorderId, _colorBorder);
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