using System;
using MisterGames.Character.Core;
using MisterGames.Character.View;
using MisterGames.Common.Attributes;
using MisterGames.Common.Easing;
using MisterGames.Common.GameObjects;
using MisterGames.Tweens;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshControllerGroup : MonoBehaviour {

        [SerializeField] private FleshController[] _fleshControllers;
        [SerializeField] private GameObject[] _enableGameObjects;
        [SerializeField] [Range(0f, 1f)] private float _progress;

        [Header("Camera")]
        [SerializeField] private float _fovWeight = 1f;
        [SerializeField] private float _fovOffset = 20f;
        [SerializeField] private AnimationCurve _fovCurve = EasingType.Linear.ToAnimationCurve();
        [SerializeField] private AnimationCurve _fovWeightCurve = EasingType.Linear.ToAnimationCurve();
        
        [Header("Effects")]
        [SerializeField] private Effect[] _effects;

        [Serializable]
        private struct Effect {
            public AnimationCurve progressCurve;
            [SerializeReference] [SubclassSelector] public ITweenProgressAction progressAction;
        }

        private CameraContainer _cameraContainer;
        private bool _hasCameraState;
        private int _cameraStateId;
        
        private void Awake() {
            _cameraContainer = CharacterSystem.Instance.GetCharacter().GetComponent<CameraContainer>();
            
            ApplyProgress(_progress);
        }

        private void OnDisable() {
            if (!_hasCameraState) return;

            _hasCameraState = false;
            _cameraContainer.RemoveState(_cameraStateId);
        }

        public void ApplyProgress(float progress) {
            _progress = progress;
            
            ApplyFlesh(progress);
            ApplyEffects(progress);
            ApplyCameraEffects(progress);
        }

        private void ApplyFlesh(float progress) {
            for (int i = 0; i < _fleshControllers.Length; i++) {
                _fleshControllers[i].ApplyProgress(progress);
            }
        }

        private void ApplyEffects(float progress) {
            _enableGameObjects.SetActive(progress > 0f);
            
            for (int i = 0; i < _effects.Length; i++) {
                ref var effect = ref _effects[i];
                effect.progressAction?.OnProgressUpdate(effect.progressCurve.Evaluate(progress));   
            }
        }

        private void ApplyCameraEffects(float progress) {
            if (progress <= 0f) {
                if (_hasCameraState) _cameraContainer.RemoveState(_cameraStateId);
                _hasCameraState = false;
                return;
            }

            if (!_hasCameraState) {
                _cameraStateId = _cameraContainer.CreateState();
                _hasCameraState = true;
            }
            
            float fov = _fovCurve.Evaluate(progress) * _fovOffset;
            float weight = _fovWeightCurve.Evaluate(progress) * _fovWeight;
            
            _cameraContainer.SetFovOffset(_cameraStateId, weight, fov);
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!Application.isPlaying || _cameraContainer == null) return;
            
            ApplyProgress(_progress);
        }
#endif
    }
    
}