using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors.Actions;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Easing;
using MisterGames.Common.GameObjects;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Rendering;

namespace _Project.Scripts.Runtime.Calibration {
    
    public sealed class CalibrationBehaviour : MonoBehaviour {

        [Header("Events")]
        [SerializeField] private EventReference _calibrationStartEvent;
        [SerializeField] private EventReference _calibrationFinishEvent;
        [SerializeField] private EventReference _itemFoundEvent;
        [SerializeField] [Min(0)] private int _itemsToFind = 3;
        
        [Header("Volume")]
        [SerializeField] private Volume _calibrationVolume;
        [SerializeField] [Min(0f)] private float _transition = 1f;
        [SerializeField] private AnimationCurve _transitionCurve = EasingType.Linear.ToAnimationCurve();

        [Header("Objects")]
        [SerializeField] private bool _disableAtStart;
        [SerializeField] private GameObject[] _enableOnFinish;

        [Header("Actions")]
        [SerializeReference] [SubclassSelector] private IActorAction _onItemFound;
        [SerializeReference] [SubclassSelector] private IActorAction _onStart;
        [SerializeReference] [SubclassSelector] private IActorAction _onFinish;
        
        private CancellationTokenSource _enableCts;
        private int _itemsFound = 0;
        private byte _transitionId;

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            
            if (_itemsToFind <= 0) {
                _calibrationVolume.weight = 0f;
                _calibrationStartEvent.Raise();    
                _calibrationFinishEvent.Raise();
                _onFinish?.Apply(null, destroyCancellationToken).Forget();
                return;
            }
            
            _calibrationVolume.weight = 1f;
            _itemsFound = 0;
            
            _itemFoundEvent.Subscribe(OnItemFound);
            
            _calibrationStartEvent.Raise();
            _onStart?.Apply(null, destroyCancellationToken).Forget();

            if (_disableAtStart) _enableOnFinish.SetEnabled(false);
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);

            _itemFoundEvent.Unsubscribe(OnItemFound);
            _calibrationVolume.weight = 0f;
        }

        private void OnItemFound() {
            _itemsFound++;
            VolumeTransition(Mathf.Clamp01(1f - (float) _itemsFound / _itemsToFind), _enableCts.Token).Forget();
            
            if (_itemsFound >= _itemsToFind) {
                _calibrationFinishEvent.Raise();
                _onFinish?.Apply(null, destroyCancellationToken).Forget();
                _enableOnFinish.SetEnabled(true);
                return;
            }
            
            _onItemFound?.Apply(null, destroyCancellationToken).Forget();
        }

        private async UniTask VolumeTransition(float targetWeight, CancellationToken cancellationToken) {
            byte id = ++_transitionId;

            float startWeight = _calibrationVolume.weight;

            float duration = _transition * Mathf.Abs(targetWeight - startWeight) * _itemsToFind;
            float speed = duration > 0f ? 1f / duration : float.MaxValue;
            float t = 0f;

            while (t < 1f && !cancellationToken.IsCancellationRequested && id == _transitionId) {
                t = Mathf.Clamp01(t + Time.deltaTime * speed);
                
                _calibrationVolume.weight = Mathf.Lerp(startWeight, targetWeight, _transitionCurve.Evaluate(t));
                
                await UniTask.Yield();
            }
        } 
    }
    
}