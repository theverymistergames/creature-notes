using System;
using MisterGames.Actors;
using MisterGames.Character.View;
using MisterGames.Common.Attributes;
using MisterGames.Common.Data;
using MisterGames.Common.Maths;
using MisterGames.Common.Tick;
using UnityEngine;

namespace _Project.Scripts.Runtime.Fireball {
    
    public sealed class FireballCameraController : MonoBehaviour, IActorComponent, IUpdate {
        
        [Header("Fire")]
        [SerializeField] [Min(0f)] private float _fireDurationMin;
        [SerializeField] [Min(0f)] private float _fireDurationMax;
        [SerializeField] [Range(0f, 1f)] private float _startFireProgressMax;
        [SerializeField] private CameraSetting _fireSetting;
        
        [Header("Stages")]
        [SerializeField] private float _weight = 1f;
        [SerializeField] [Min(0f)] private float _defaultSmoothing = 5f;
        [SerializeField] private StageSetting[] _stages;
        [SerializeField] private TransitionSetting[] _transitions;
        
        [Serializable]
        private struct StageSetting {
            public Optional<FireballShootingBehaviour.Stage> previousStage;
            public FireballShootingBehaviour.Stage currentStage;
            public CameraSetting setting;
        }
        
        [Serializable]
        private struct TransitionSetting {
            public Optional<FireballShootingBehaviour.Stage> previousStage;
            public FireballShootingBehaviour.Stage currentStage;
            [Min(0f)] public float duration;
            public CameraSetting setting;
        }

        [Serializable]
        private sealed class CameraSetting {
            [Header("Smoothing")]
            [Min(0f)] public float smoothingStart;
            [Min(0f)] public float smoothingEnd;
            
            [Header("Motion")]
            public FloatParameter fov = FloatParameter.Default();
            public Vector3Parameter position = Vector3Parameter.Default();
            public Vector3Parameter rotation = Vector3Parameter.Default();
            
            [Header("Noise")]
            public Vector3 positionNoiseOffset;
            public Vector3 rotationNoiseOffset;
            public Vector3Parameter noiseSpeed = Vector3Parameter.Default();
            public Vector3Parameter positionNoise = Vector3Parameter.Default();
            public Vector3Parameter rotationNoise = Vector3Parameter.Default();
        }
        
        private FireballShootingBehaviour _fireballBehaviour;
        private CameraContainer _cameraContainer;
        private CameraShaker _cameraShaker;
        
        private CameraSetting _defaultSetting;
        private CameraSetting _currentSetting;
        private CameraSetting _stageSetting;
        private CameraSetting _transitionSetting;
        private CameraSetting _overrideSetting;
        
        private int _stageShakeId;
        private int _stageMotionId;
        
        private float _fovMul;
        private Vector3 _positionMul;
        private Vector3 _rotationMul;
        private Vector3 _noiseSpeedMul;
        private Vector3 _positionNoiseMul;
        private Vector3 _rotationNoiseMul;

        private float _fov;
        private Vector3 _position;
        private Vector3 _rotation;
        private Vector3 _noiseSpeed;
        private Vector3 _positionNoise;
        private Vector3 _rotationNoise;
        
        private float _transitionProgress;
        private float _transitionSpeed;
        
        private float _overrideProgress;
        private float _overrideSpeed;
        
        void IActorComponent.OnAwake(IActor actor) {
            _cameraContainer = actor.GetComponent<CameraContainer>();
            _cameraShaker = actor.GetComponent<CameraShaker>();
            _fireballBehaviour = actor.GetComponent<FireballShootingBehaviour>();
            
            InitializeDefaultSetting();
        }

        private void OnEnable() {
            ResetParameters();

            _stageMotionId = _cameraContainer.CreateState();
            _stageShakeId = _cameraShaker.CreateState(_weight);

            _fireballBehaviour.OnStageChanged += OnStageChanged;
            _fireballBehaviour.OnFire += OnFire;
            
            PlayerLoopStage.LateUpdate.Subscribe(this);
        }

        private void OnDisable() {
            _cameraContainer.RemoveState(_stageMotionId);
            _cameraShaker.RemoveState(_stageShakeId);

            _fireballBehaviour.OnStageChanged -= OnStageChanged;
            _fireballBehaviour.OnFire -= OnFire;
            
            PlayerLoopStage.LateUpdate.Unsubscribe(this);
        }

        private void InitializeDefaultSetting() {
            _defaultSetting = new CameraSetting {
                smoothingStart = _defaultSmoothing,
                smoothingEnd = _defaultSmoothing,
            };
        }

        private void ResetParameters() {
            _stageSetting = _defaultSetting;
            _transitionSetting = _defaultSetting;
            _overrideSetting = _defaultSetting;
            
            _fovMul = default;
            _positionMul = default;
            _rotationMul = default;
            _noiseSpeedMul = default;
            _positionNoiseMul = default;
            _rotationNoiseMul = default;

            _fov = default;
            _position = default;
            _rotation = default;
            _noiseSpeed = default;
            _positionNoise = default;
            _rotationNoise = default;

            _transitionProgress = 1f;
            _transitionSpeed = 0f;

            _overrideProgress = 1f;
            _overrideSpeed = 0f;
        }

        private void OnFire(float progress) {
            _overrideSetting = _fireSetting;
            
            float duration = _fireDurationMin + progress * (_fireDurationMax - _fireDurationMin);
            _overrideProgress = Mathf.Min(_startFireProgressMax, 1f - progress);
            _overrideSpeed = duration > 0f ? 1f / duration : float.MaxValue;
        }

        private void OnStageChanged(FireballShootingBehaviour.Stage previous, FireballShootingBehaviour.Stage current) {
            UpdateStageSetting(previous, current);
            UpdateTransitionSetting(previous, current);
        }

        private void UpdateStageSetting(FireballShootingBehaviour.Stage previous, FireballShootingBehaviour.Stage current) {
            for (int i = 0; i < _stages.Length; i++) {
                ref var stage = ref _stages[i];
                
                if (stage.currentStage != current ||
                    stage.previousStage.HasValue && stage.previousStage.Value != previous
                ) {
                    continue;
                }

                _stageSetting = stage.setting;  
                return;
            }

            _stageSetting = _defaultSetting;
        }

        private void UpdateTransitionSetting(FireballShootingBehaviour.Stage previous, FireballShootingBehaviour.Stage current) {
            for (int i = 0; i < _transitions.Length; i++) {
                ref var transition = ref _transitions[i];
                
                if (transition.currentStage != current ||
                    transition.previousStage.HasValue && transition.previousStage.Value != previous
                ) {
                    continue;
                }

                _transitionSetting = transition.setting;
                _transitionSpeed = transition.duration > 0f ? 1f / transition.duration : float.MaxValue;
                _transitionProgress = 0f;
                return;
            }
        }

        void IUpdate.OnUpdate(float dt) {
            _transitionProgress = Mathf.Clamp01(_transitionProgress + dt * _transitionSpeed);
            _overrideProgress = Mathf.Clamp01(_overrideProgress + dt * _overrideSpeed);

            CameraSetting setting;
            float progress;

            if (_overrideProgress < 1f) {
                setting = _overrideSetting;
                progress = _overrideProgress;
            }
            else if (_transitionProgress < 1f) {
                setting = _transitionSetting;
                progress = _transitionProgress;
            }
            else {
                setting = _stageSetting;
                progress = _fireballBehaviour.StageProgress;
            }

            if (_currentSetting != setting) SetupMultipliers(setting);
            _currentSetting = setting;
            
            ProcessCameraSetting(setting, progress, dt);
        }

        private void ProcessCameraSetting(CameraSetting setting, float progress, float dt) {
            float fov = setting.fov.Evaluate(progress) * _fovMul;
            var position = setting.position.Evaluate(progress).Multiply(_positionMul);
            var rotation = setting.rotation.Evaluate(progress).Multiply(_rotationMul);
            
            var noiseSpeed = setting.noiseSpeed.Evaluate(progress).Multiply(_noiseSpeedMul);
            var positionNoise = setting.positionNoise.Evaluate(progress).Multiply(_positionNoiseMul);
            var rotationNoise = setting.rotationNoise.Evaluate(progress).Multiply(_rotationNoiseMul);

            float smoothing = setting.smoothingStart + progress * (setting.smoothingEnd - setting.smoothingStart);

            _fov = _fov.SmoothExp(fov, dt * smoothing);
            _position = _position.SmoothExp(position, dt * smoothing);
            _rotation = _rotation.SmoothExp(rotation, dt * smoothing);

            _noiseSpeed = _noiseSpeed.SmoothExp(noiseSpeed, dt * smoothing);
            _positionNoise = _positionNoise.SmoothExp(positionNoise, dt * smoothing);
            _rotationNoise = _rotationNoise.SmoothExp(rotationNoise, dt * smoothing);
            
            _cameraContainer.SetFovOffset(_stageMotionId, _weight, _fov);
            _cameraContainer.SetPositionOffset(_stageMotionId, _weight, _position);
            _cameraContainer.SetRotationOffset(_stageMotionId, _weight, Quaternion.Euler(_rotation));
            
            _cameraShaker.SetSpeed(_stageShakeId, _noiseSpeed);
            _cameraShaker.SetPosition(_stageShakeId, setting.positionNoiseOffset, _positionNoise);
            _cameraShaker.SetRotation(_stageShakeId, setting.rotationNoiseOffset, _rotationNoise);
        }

        private void SetupMultipliers(CameraSetting setting) {
            _fovMul = setting.fov.CreateMultiplier();
            _positionMul = setting.position.CreateMultiplier();
            _rotationMul = setting.rotation.CreateMultiplier();
            
            _noiseSpeedMul = setting.noiseSpeed.CreateMultiplier();
            _positionNoiseMul = setting.positionNoise.CreateMultiplier();
            _rotationNoiseMul = setting.rotationNoise.CreateMultiplier();
        }

#if UNITY_EDITOR
        [Button] private void Fire0() => OnFire(0f);
        [Button] private void Fire25() => OnFire(0.25f);
        [Button] private void Fire50() => OnFire(0.5f);
        [Button] private void Fire75() => OnFire(0.75f);
        [Button] private void Fire100() => OnFire(1f);
#endif
    }
    
}