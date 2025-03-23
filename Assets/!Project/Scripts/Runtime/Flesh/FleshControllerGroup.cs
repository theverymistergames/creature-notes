using MisterGames.Common.Attributes;
using MisterGames.Common.Easing;
using MisterGames.Tweens;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshControllerGroup : MonoBehaviour {

        [SerializeField] private FleshController[] _fleshControllers;
        [SerializeField] [Range(0f, 1f)] private float _progress;
        
        [Header("Effects")]
        [SerializeField] private AnimationCurve _progressCurve = EasingType.Linear.ToAnimationCurve();
        [SerializeReference] [SubclassSelector] private ITweenProgressAction _progressAction;
        
        private void Awake() {
            ApplyProgress(_progress);
        }
        
        public void ApplyProgress(float progress) {
            _progress = progress;

            for (int i = 0; i < _fleshControllers.Length; i++) {
                _fleshControllers[i].ApplyProgress(progress);
            }
            
            _progressAction?.OnProgressUpdate(_progressCurve.Evaluate(progress));
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (Application.isPlaying) ApplyProgress(_progress);
        }
#endif
    }
    
}