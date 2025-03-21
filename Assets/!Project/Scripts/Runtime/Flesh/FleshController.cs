using MisterGames.Common.Maths;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshController : MonoBehaviour {

        [Header("Positioning")]
        [SerializeField] private Transform _fleshRoot;
        [SerializeField] private float _bottomY;
        [SerializeField] private float _topY;
        [SerializeField] [Range(0f, 1f)] private float _progress;

        public Transform Root => _fleshRoot;
        public float Progress => _progress;

        public void ApplyProgress(float progress) {
            _progress = progress;
            _fleshRoot.localPosition = _fleshRoot.localPosition.WithY(Mathf.Lerp(_bottomY, _topY, progress));
        }

#if UNITY_EDITOR
        [SerializeField] private bool _applyProgressInEditor;
        
        private void OnValidate() {
            if (_applyProgressInEditor && _fleshRoot != null) ApplyProgress(_progress);
        }
#endif
    }
    
}