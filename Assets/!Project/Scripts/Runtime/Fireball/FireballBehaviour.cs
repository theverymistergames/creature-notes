using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Character.View;
using MisterGames.Common.Async;
using MisterGames.Common.GameObjects;
using MisterGames.Common.Pooling;
using MisterGames.Input.Actions;
using MisterGames.Tick.Core;
using UnityEngine;

namespace _Project.Scripts.Runtime.Fireball {
    
    public sealed class FireballBehaviour : MonoBehaviour, IActorComponent, IUpdate {
        
        [Header("Inputs")]
        [SerializeField] private InputActionKey _chargeInput;
        [SerializeField] private InputActionKey _fireInput;

        [Header("Visuals")]
        [SerializeField] private GameObject[] _enableGameObjects;
        [SerializeField] [Min(0f)] private float _disableDelay;
        
        public enum Stage {
            None,
            Prepare,
            Charge,
            Overheat,
            Cooldown,
        }

        public delegate void StageChange(Stage old, Stage current); 
        
        public event StageChange OnStageChanged = delegate { };
        public event Action<Stage> OnCannotCharge = delegate { };
        
        public Stage CurrentStage { get; private set; }
        public float StageProgress { get; private set; }
        public float StageDuration { get; private set; }
        
        private CancellationTokenSource _enableCts;
        private CharacterViewPipeline _view;
        private FireballShootingData _shootingData;
        private float _stageSpeed;
        private byte _visualsEnableId;

        void IActorComponent.OnAwake(IActor actor) {
            _view = actor.GetComponent<CharacterViewPipeline>();
        }

        void IActorComponent.OnSetData(IActor actor) {
            _shootingData = actor.GetData<FireballShootingData>();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            
            StageProgress = 0f;
            _stageSpeed = 0f;
            CurrentStage = Stage.None;
            
            _enableGameObjects.SetActive(false);
            
            PlayerLoopStage.Update.Subscribe(this);
            
            _chargeInput.OnPress += OnChargeInputPressed;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            
            PlayerLoopStage.Update.Unsubscribe(this);
            
            _chargeInput.OnPress -= OnChargeInputPressed;
            
            _enableGameObjects.SetActive(false);
        }

        void IUpdate.OnUpdate(float dt) {
            ProcessCurrentStage(dt);
        }

        private void ProcessCurrentStage(float dt) {
            StageProgress = Mathf.Clamp01(StageProgress + dt * _stageSpeed);

            var oldStage = CurrentStage;
            CurrentStage = CurrentStage switch {
                Stage.None => ProcessNoneStage(),
                Stage.Prepare => ProcessPrepareStage(StageProgress),
                Stage.Charge => ProcessChargeStage(StageProgress),
                Stage.Overheat => ProcessOverheatStage(StageProgress),
                Stage.Cooldown => ProcessCooldownStage(StageProgress),
                _ => throw new ArgumentOutOfRangeException()
            };
            
            if (oldStage == CurrentStage) return;
            
            NotifyEnableVisuals(CurrentStage != Stage.None);
            
            OnStageChanged.Invoke(oldStage, CurrentStage);
        }

        private Stage ProcessNoneStage() {
            return _chargeInput.IsPressed 
                ? StartNextStage(Stage.Prepare, _shootingData.prepareDuration) 
                : Stage.None;
        }

        private Stage ProcessPrepareStage(float progress) {
            if (!_chargeInput.IsPressed) {
                return StartNextStage(Stage.None);
            }
            
            return progress < 1f 
                ? Stage.Prepare 
                : StartNextStage(Stage.Charge, _shootingData.chargeDuration);
        }
        
        private Stage ProcessChargeStage(float progress) {
            if (_fireInput.WasUsed) {
                PerformShot(progress);
                return StartNextStage(Stage.Cooldown, _shootingData.cooldownDuration);
            }
            
            if (!_chargeInput.IsPressed) {
                return StartNextStage(Stage.Cooldown, _shootingData.cooldownDuration);
            }
                
            return progress < 1f 
                ? Stage.Charge 
                : StartNextStage(Stage.Overheat, _shootingData.overheatDuration);
        }
        
        private Stage ProcessOverheatStage(float progress) {
            return progress < 1f 
                ? Stage.Overheat 
                : StartNextStage(Stage.Cooldown, _shootingData.overheatCooldownDuration);
        }
        
        private Stage ProcessCooldownStage(float progress) {
            return progress < 1f 
                ? Stage.Cooldown 
                : StartNextStage(Stage.None);
        }

        private Stage StartNextStage(Stage stage, float duration = 0f) {
            StageProgress = 0f;
            StageDuration = duration;
            
            _stageSpeed = stage != Stage.None 
                ? duration > 0f ? 1f / duration : float.MaxValue 
                : 0f;
            
            return stage;
        }

        private void PerformShot(float progress) {
            var pos = _view.HeadPosition;
            var orient = _view.Orientation;
            
            var shotRb = PrefabPool.Main.Get(_shootingData.shotPrefab, pos + orient * _shootingData.spawnOffset, orient);

            float force = _shootingData.forceMin + 
                          _shootingData.forceByChargeProgress.Evaluate(progress) * (_shootingData.forceMax - _shootingData.forceMax);
            
            float angle = _shootingData.angleMin + 
                          _shootingData.angleByChargeProgress.Evaluate(progress) * (_shootingData.angleMax - _shootingData.angleMin);

            
            shotRb.velocity = Quaternion.AngleAxis(angle, orient * Vector3.left) * orient * (Vector3.forward * force);
        }
        
        private void OnChargeInputPressed() {
            if (CurrentStage is Stage.Overheat or Stage.Cooldown) {
                OnCannotCharge.Invoke(CurrentStage);
            }
        }

        private void NotifyEnableVisuals(bool active) {
            byte id = ++_visualsEnableId;
            
            if (active) {
                _enableGameObjects.SetActive(true);
                return;
            }

            DisableVisualsAfterDelay(id, _disableDelay, _enableCts.Token).Forget();
        }

        private async UniTask DisableVisualsAfterDelay(byte id, float delay, CancellationToken cancellationToken) {
            await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken)
                .SuppressCancellationThrow();
            
            if (id != _visualsEnableId || cancellationToken.IsCancellationRequested) return;
            
            _enableGameObjects.SetActive(false);
        }
    }
    
}