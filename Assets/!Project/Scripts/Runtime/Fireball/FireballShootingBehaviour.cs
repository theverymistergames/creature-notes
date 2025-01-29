using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Character.View;
using MisterGames.Collisions.Utils;
using MisterGames.Common.Async;
using MisterGames.Common.GameObjects;
using MisterGames.Common.Pooling;
using MisterGames.Input.Actions;
using MisterGames.Common.Tick;
using UnityEngine;

namespace _Project.Scripts.Runtime.Fireball {
    
    public sealed class FireballShootingBehaviour : MonoBehaviour, IActorComponent, IUpdate {
        
        [Header("Inputs")]
        [SerializeField] private InputActionKey _chargeInput;
        [SerializeField] private InputActionKey _fireInput;

        [Header("Shoot Position")]
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] [Min(0)] private int _maxHits = 4;
        
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

        public delegate void StageChange(Stage previous, Stage current); 
        public delegate void FireCallback(float progress); 
        
        public event StageChange OnStageChanged = delegate { };
        public event Action<Stage> OnCannotCharge = delegate { };
        public event FireCallback OnFire = delegate { };
        
        public Stage CurrentStage { get; private set; }
        public float StageProgress { get; private set; }
        public float StageDuration { get; private set; }

        private RaycastHit[] _hits;
        private CancellationTokenSource _enableCts;
        private IActor _actor;
        private CharacterViewPipeline _view;
        private FireballShootingData _shootingData;
        private float _stageSpeed;
        private byte _visualsEnableId;

        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
            _view = actor.GetComponent<CharacterViewPipeline>();
            
            _hits = new RaycastHit[_maxHits];
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

        public void ForceStage(Stage stage, float progress = 0f) {
            var oldStage = CurrentStage;
            CurrentStage = StartNextStage(stage, _shootingData.overheatDuration, progress);

            if (oldStage == CurrentStage) return;
            
            NotifyEnableVisuals(CurrentStage != Stage.None);
            OnStageChanged.Invoke(oldStage, CurrentStage);
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
            return CanCharge() 
                ? StartNextStage(Stage.Prepare, _shootingData.prepareDuration) 
                : Stage.None;
        }

        private Stage ProcessPrepareStage(float progress) {
            if (!CanCharge()) {
                return StartNextStage(Stage.None, _shootingData.noneDuration);
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
            
            if (!CanCharge()) {
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
                : StartNextStage(Stage.None, _shootingData.noneDuration);
        }

        private Stage StartNextStage(Stage stage, float duration = 0f, float progress = 0f) {
            StageProgress = Mathf.Clamp01(progress);
            StageDuration = duration;
            
            _stageSpeed = duration > 0f ? 1f / duration : float.MaxValue;
            
            return stage;
        }

        private void PerformShot(float progress) {
            var orient = _view.Rotation;
            var pos = _view.Position;

            Actor prefab = null;

            for (int i = 0; i < _shootingData.shotPrefabs.Length; i++) {
                ref var data = ref _shootingData.shotPrefabs[i];
                if (data.chargeProgress < progress) continue;

                prefab = data.shotPrefab;
                break;
            }
            
            if (prefab == null) return;

            var shotActor = PrefabPool.Main.Get(prefab, GetShootPosition(pos, orient), orient);
            shotActor.ParentActor = _actor;
            shotActor.Transform.localScale = prefab.transform.localScale * 
                                             Mathf.Lerp(_shootingData.scaleStart, _shootingData.scaleEnd, _shootingData.scaleByChargeProgress.Evaluate(progress));
            
            if (shotActor.TryGetComponent(out Rigidbody rb)) {
                float force = Mathf.Lerp(_shootingData.forceStart, _shootingData.forceEnd, _shootingData.forceByChargeProgress.Evaluate(progress));
                float angle = Mathf.Lerp(_shootingData.angleStart, _shootingData.angleEnd, _shootingData.angleByChargeProgress.Evaluate(progress));
            
                rb.linearVelocity = Quaternion.AngleAxis(angle, orient * Vector3.left) * orient * (Vector3.forward * force);
                rb.rotation = Quaternion.LookRotation(rb.linearVelocity);
            }
            
            OnFire.Invoke(progress);
        }

        private Vector3 GetShootPosition(Vector3 origin, Quaternion orientation) {
            var targetPoint = origin + orientation * _shootingData.spawnOffset;
            
            var dir = targetPoint - origin;
            float distance = dir.magnitude;
            dir.Normalize();
            
            int hitCount = Physics.RaycastNonAlloc(origin, dir, _hits, distance, _obstacleLayer, QueryTriggerInteraction.Ignore);

            return _hits.TryGetMinimumDistanceHit(hitCount, out var hit) ? hit.point : targetPoint;
        }
        
        private void OnChargeInputPressed() {
            if (CurrentStage is Stage.Overheat or Stage.Cooldown) {
                OnCannotCharge.Invoke(CurrentStage);
            }
        }

        private bool CanCharge() {
            return _chargeInput.IsPressed;
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