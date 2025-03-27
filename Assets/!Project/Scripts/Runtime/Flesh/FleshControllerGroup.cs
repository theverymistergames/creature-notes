using System;
using MisterGames.Common.Attributes;
using MisterGames.Tweens;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshControllerGroup : MonoBehaviour {

        [SerializeField] private FleshController[] _fleshControllers;
        [SerializeField] [Range(0f, 1f)] private float _progress;

        [Header("Effects")]
        [SerializeField] private Effect[] _effects;

        [Serializable]
        private struct Effect {
            public AnimationCurve progressCurve;
            [SerializeReference] [SubclassSelector] public ITweenProgressAction progressAction;
        }
        
        private void Awake() {
            ApplyProgress(_progress);
        }
        
        public void ApplyProgress(float progress) {
            _progress = progress;

            for (int i = 0; i < _fleshControllers.Length; i++) {
                _fleshControllers[i].ApplyProgress(progress);
            }

            for (int i = 0; i < _effects.Length; i++) {
                ref var effect = ref _effects[i];
                effect.progressAction?.OnProgressUpdate(effect.progressCurve.Evaluate(progress));   
            }
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (Application.isPlaying) ApplyProgress(_progress);
        }
#endif
    }
    
}