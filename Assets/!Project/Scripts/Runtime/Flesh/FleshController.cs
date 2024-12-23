using MisterGames.Common.Maths;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshController : MonoBehaviour {

        [Header("Positioning")]
        [SerializeField] private Transform _fleshRoot;
        [SerializeField] private float _bottomY;
        [SerializeField] private float _topY;
        
        [Header("Debug")]
        [SerializeField] [Range(0f, 1f)] private float _progress;

        public Transform Root => _fleshRoot;
        public float Progress => _progress;

        public void ApplyProgress(float progress) {
            _progress = progress;
            _fleshRoot.position = _fleshRoot.position.WithY(Mathf.Lerp(_bottomY, _topY, progress));
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!Application.isPlaying && _fleshRoot != null) ApplyProgress(_progress);
        }
#endif
    }
    
}