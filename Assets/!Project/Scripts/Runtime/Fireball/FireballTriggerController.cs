using System;
using MisterGames.Actors;
using MisterGames.Common.Data;
using MisterGames.Common.Easing;
using MisterGames.Common.Inputs;
using MisterGames.Common.Inputs.DualSense;
using MisterGames.Common.Tick;
using UnityEngine;

namespace _Project.Scripts.Runtime.Fireball {

    public sealed class FireballTriggerController : MonoBehaviour, IActorComponent, IUpdate {

        [SerializeField] private GamepadSide _trigger = GamepadSide.Right;
        [SerializeField] private TriggerSetting _defaultSetting;
        [SerializeField] private StageSetting[] _stages;

        [Serializable]
        private struct StageSetting {
            public Optional<FireballShootingBehaviour.Stage> previousStage;
            public FireballShootingBehaviour.Stage currentStage;
            public TriggerSetting setting;
        }

        [Serializable]
        private sealed class TriggerSetting {
            public AnimationCurve curve = EasingType.Linear.ToAnimationCurve();
            public TriggerEffect triggerEffectStart;
            public Optional<TriggerEffect> triggerEffectEnd;
        }

        private FireballShootingBehaviour _fireballBehaviour;
        private TriggerSetting _stageSetting;

        void IActorComponent.OnAwake(IActor actor) {
            _fireballBehaviour = actor.GetComponent<FireballShootingBehaviour>();
        }

        private void OnEnable() {
            ResetParameters();
            ApplySetting(_stageSetting, 0f);
            
            _fireballBehaviour.OnStageChanged += OnStageChanged;
        }

        private void OnDisable() {
            _fireballBehaviour.OnStageChanged -= OnStageChanged;
            PlayerLoopStage.LateUpdate.Unsubscribe(this);
        }

        private void ResetParameters() {
            _stageSetting = _defaultSetting;
        }

        private void OnStageChanged(FireballShootingBehaviour.Stage previous, FireballShootingBehaviour.Stage current) {
            UpdateStageSetting(previous, current);
            PlayerLoopStage.LateUpdate.Subscribe(this);
        }

        private void UpdateStageSetting(FireballShootingBehaviour.Stage previous, FireballShootingBehaviour.Stage current) {
            for (int i = 0; i < _stages.Length; i++) {
                ref var stage = ref _stages[i];

                if (stage.currentStage != current ||
                    stage.previousStage.HasValue && stage.previousStage.Value != previous) 
                {
                    continue;
                }

                _stageSetting = stage.setting;
                return;
            }

            _stageSetting = _defaultSetting;
        }

        void IUpdate.OnUpdate(float dt) {
            if (!_stageSetting.triggerEffectEnd.HasValue) {
                ApplySetting(_stageSetting, 0f);
                PlayerLoopStage.LateUpdate.Unsubscribe(this);
                return;
            }

            float progress = _fireballBehaviour.StageProgress;
            ApplySetting(_stageSetting, progress);
            
            if (progress >= 1f) PlayerLoopStage.LateUpdate.Unsubscribe(this);
        }

        private void ApplySetting(TriggerSetting setting, float progress) {
            var effect = setting.triggerEffectStart;

            if (progress > 0f) {
                var endEffect = setting.triggerEffectEnd.Value;
                float p = setting.curve.Evaluate(progress);
                
                effect.StartPosition = Mathf.Lerp((float) effect.StartPosition, (float) endEffect.StartPosition, p);
                effect.EndPosition = Mathf.Lerp((float) effect.EndPosition, (float) endEffect.EndPosition, p);
                effect.BeginForce = Mathf.Lerp((float) effect.BeginForce, (float) endEffect.BeginForce, p);
                effect.MiddleForce = Mathf.Lerp((float) effect.MiddleForce, (float) endEffect.MiddleForce, p);
                effect.EndForce = Mathf.Lerp((float) effect.EndForce, (float) endEffect.EndForce, p);
                effect.Frequency = Mathf.Lerp((float) effect.Frequency, (float) endEffect.Frequency, p);
            }
            
            DeviceService.Instance.DualSenseAdapter.SetTriggerEffect(_trigger, effect);
        }
    }
    
}