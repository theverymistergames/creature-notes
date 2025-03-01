using System;
using MisterGames.Actors;
using MisterGames.Common.Attributes;
using MisterGames.Common.Data;
using MisterGames.Common.Easing;
using MisterGames.Common.Inputs;
using MisterGames.Common.Labels;
using MisterGames.Common.Tick;
using UnityEngine;

namespace _Project.Scripts.Runtime.Fireball {
    
    public sealed class FireballVibrationController : MonoBehaviour, IActorComponent, IUpdate {

        [Header("Priority")]
        [SerializeField] private LabelValue _priority;
        
        [Header("Fire")]
        [SerializeField] [Min(0f)] private float _fireDurationMin;
        [SerializeField] [Min(0f)] private float _fireDurationMax;
        [SerializeField] private AnimationCurve _fireAmplitudeByProgress = EasingType.Linear.ToAnimationCurve();
        [SerializeField] private VibrationSetting _fireSetting;
        
        [Header("Fire Fail")]
        [SerializeField] [Min(0f)] private float _fireFailDuration;
        [SerializeField] private VibrationSetting _fireFailSetting;
        
        [Header("Stages")]
        [SerializeField] private StageSetting[] _stages;
        
        [Serializable]
        private struct StageSetting {
            public Optional<FireballShootingBehaviour.Stage> previousStage;
            public FireballShootingBehaviour.Stage currentStage;
            public VibrationSetting setting;
        }
        
        [Serializable]
        private sealed class VibrationSetting {
            [Min(0f)] public float weight = 1f;
            [Min(0f)] public float amplitude = 1f;
            public OscillatedCurve curveLeft;
            public OscillatedCurve curveRight;
        }
        
        private FireballShootingBehaviour _fireballBehaviour;
        
        private VibrationSetting _defaultSetting;
        private VibrationSetting _stageSetting;
        private VibrationSetting _overrideSetting;

        private float _overrideProgress;
        private float _overrideSpeed;
        private float _overrideAmplitude;

        void IActorComponent.OnAwake(IActor actor) {
            _fireballBehaviour = actor.GetComponent<FireballShootingBehaviour>();
            
            InitializeDefaultSetting();
        }

        private void OnEnable() {
            ResetParameters();

            DeviceService.Instance.GamepadVibration.Register(this, _priority.GetValue());

            _fireballBehaviour.OnStageChanged += OnStageChanged;
            _fireballBehaviour.OnFire += OnFire;
            _fireballBehaviour.OnCannotCharge += OnCannotCharge;
            
            PlayerLoopStage.LateUpdate.Subscribe(this);
        }

        private void OnDisable() {
            DeviceService.Instance.GamepadVibration.Unregister(this);

            _fireballBehaviour.OnStageChanged -= OnStageChanged;
            _fireballBehaviour.OnFire -= OnFire;
            _fireballBehaviour.OnCannotCharge -= OnCannotCharge;
            
            PlayerLoopStage.LateUpdate.Unsubscribe(this);
        }

        private void InitializeDefaultSetting() {
            _defaultSetting = new VibrationSetting();
        }

        private void ResetParameters() {
            _stageSetting = _defaultSetting;
            _overrideSetting = _defaultSetting;
            
            _overrideProgress = 1f;
            _overrideSpeed = 0f;
        }

        private void OnFire(float progress) {
            _overrideSetting = _fireSetting;
            _overrideAmplitude = _fireAmplitudeByProgress.Evaluate(progress);
            
            float duration = _fireDurationMin + progress * (_fireDurationMax - _fireDurationMin);
            _overrideProgress = 0f;
            _overrideSpeed = duration > 0f ? 1f / duration : float.MaxValue;
        }
        
        private void OnCannotCharge(FireballShootingBehaviour.Stage stage) {
            if (stage != FireballShootingBehaviour.Stage.Cooldown) return;
            
            _overrideSetting = _fireFailSetting;
            _overrideAmplitude = 1f;
            
            _overrideProgress = 0f;
            _overrideSpeed = _fireFailDuration > 0f ? 1f / _fireFailDuration : float.MaxValue;
        }

        private void OnStageChanged(FireballShootingBehaviour.Stage previous, FireballShootingBehaviour.Stage current) {
            UpdateStageSetting(previous, current);
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

        void IUpdate.OnUpdate(float dt) {
            _overrideProgress = Mathf.Clamp01(_overrideProgress + dt * _overrideSpeed);

            VibrationSetting setting;
            float progress;
            float amplitude;
            
            if (_overrideProgress < 1f) {
                setting = _overrideSetting;
                progress = _overrideProgress;
                amplitude = _overrideAmplitude;
            }
            else {
                setting = _stageSetting;
                progress = _fireballBehaviour.StageProgress;
                amplitude = 1f;
            }

            ProcessVibrationSetting(setting, progress, amplitude);
        }

        private void ProcessVibrationSetting(VibrationSetting setting, float progress, float amplitude) {
            var freq = amplitude * setting.amplitude * new Vector2(
                setting.curveLeft.Evaluate(progress),
                setting.curveRight.Evaluate(progress)
            );
            
            DeviceService.Instance.GamepadVibration.SetTwoMotors(this, freq, setting.weight);
        }

#if UNITY_EDITOR
        [Button] private void Fire0() => OnFire(0f);
        [Button] private void Fire25() => OnFire(0.25f);
        [Button] private void Fire50() => OnFire(0.5f);
        [Button] private void Fire75() => OnFire(0.75f);
        [Button] private void Fire100() => OnFire(1f);
        [Button] private void FireFail() => OnCannotCharge(FireballShootingBehaviour.Stage.Cooldown);
#endif
    }
    
}