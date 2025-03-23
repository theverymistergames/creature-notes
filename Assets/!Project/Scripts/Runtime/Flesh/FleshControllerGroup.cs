using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshControllerGroup : MonoBehaviour {

        [SerializeField] private FleshController[] _fleshControllers;
        [SerializeField] [Range(0f, 1f)] private float _progress;

        private void Awake() {
            ApplyProgress(_progress);
        }
        
        public void ApplyProgress(float progress) {
            _progress = progress;

            for (int i = 0; i < _fleshControllers.Length; i++) {
                _fleshControllers[i].ApplyProgress(progress);
            }
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (Application.isPlaying) ApplyProgress(_progress);
        }
#endif
    }
    
}