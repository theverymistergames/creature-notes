using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Async;
using MisterGames.Common.Pooling;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using MisterGames.Tick.Core;
using UnityEngine;

namespace _Project.Scripts.Runtime.Telescope {
    
    public sealed class StarGroupDetection : MonoBehaviour, IUpdate {
        
        [SerializeField] private StarGroupsData _starGroupsData;
        [SerializeField] private Interactive _interactive;
        [SerializeField] private StarGroupsPlacer _starGroupsPlacer;
        
        [Header("Lens")]
        [SerializeField] private Transform _lensPlace;
        [SerializeField] private Vector3 _lensOffset;
        [SerializeField] private Vector3 _lensRotationOffset;
        
        [Header("Events")]
        [SerializeField] private EventReference _pickLensEvent;
        [SerializeField] private EventReference _detectLensEvent;

        private CancellationTokenSource _enableCts;
        private GameObject _currentLens;
        
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

        private void OnStartInteract(IInteractiveUser user) {
            PlayerLoopStage.Update.Subscribe(this);
            SetupLens(_enableCts.Token).Forget();
        }

        private void OnStopInteract(IInteractiveUser user) {
            PlayerLoopStage.Update.Unsubscribe(this);
        }

        private async UniTask SetupLens(CancellationToken cancellationToken) {
            if (_currentLens != null) {
                var canvas = _currentLens.GetComponentInChildren<StarGroupsCanvas>(includeInactive: true);
                if (_detectLensEvent.GetRaiseCount(canvas.SelectedStarGroupIndex) <= 0) return;

                await TakeLensOff(cancellationToken);
            }
            
            int count = _starGroupsPlacer.StarGroupCount;
            int selectedIndex = -1;
            
            for (int i = 0; i < count; i++) {
                if (_pickLensEvent.GetRaiseCount(i) <= 0 || _detectLensEvent.GetRaiseCount(i) > 0) continue;

                selectedIndex = i;
                break;
            }

            if (selectedIndex < 0) return;
            
            var lens = CreateLens(selectedIndex);
            await TakeLensOn(lens, cancellationToken);
        }

        private async UniTask TakeLensOn(GameObject lens, CancellationToken cancellationToken) {
            if (_currentLens != null) PrefabPool.Instance.Recycle(_currentLens);
            _currentLens = lens;

            var t = _currentLens.transform;
            t.parent = _lensPlace;
            
            var lensPlaceRot = _lensPlace.rotation;
            
            t.SetPositionAndRotation(
                _lensPlace.position + lensPlaceRot * _lensOffset,
                lensPlaceRot * Quaternion.Euler(_lensRotationOffset)
            );
            
            _currentLens.SetActive(true);
        }
        
        private async UniTask TakeLensOff(CancellationToken cancellationToken) {
            var lens = _currentLens;
            _currentLens = null;
            
            PrefabPool.Instance.Recycle(lens);
        }

        private GameObject CreateLens(int groupIndex) {
            var prefab = _starGroupsData.starGroups[groupIndex].lensPrefab;
            var instance = PrefabPool.Instance.TakeInactive(prefab.gameObject);
            return instance;
        }

        void IUpdate.OnUpdate(float dt) {
            
        }
    }
    
}