using MisterGames.Common.Easing;
using MisterGames.Common.Maths;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshController : MonoBehaviour {

        [SerializeField] private Transform _transform;
        [SerializeField] private Renderer _renderer;
        [SerializeField] private float _transformOffsetY = 2f;
        [SerializeField] private float _materialOffsetYStart = 0f;
        [SerializeField] private float _materialOffsetYEnd = 2f;
        [SerializeField] private AnimationCurve _curve = EasingType.Linear.ToAnimationCurve();
        [SerializeField] [Range(0f, 1f)] private float _progress;
        [SerializeField] [Range(0f, 1f)] private float _enableAtProgress;

        private static readonly int PositionOffset = Shader.PropertyToID("_Position_Offset");
        
        public Transform Root => _transform;
        public float Progress => _progress;
        public float MaterialOffsetY => Mathf.Lerp(_materialOffsetYStart, _materialOffsetYEnd, _curve.Evaluate(_progress));

        private Vector3 _materialOffsetDefault;
        private Vector3 _transformPositionDefault;
        
        private void Awake() {
            _transformPositionDefault = _transform.localPosition;
            _materialOffsetDefault = _renderer.material.GetVector(PositionOffset);
            
            ApplyProgress(_progress);
        }

        public void ApplyProgress(float progress) {
            _progress = progress;
            float t = _curve.Evaluate(progress);

            if (progress <= _enableAtProgress) {
                gameObject.SetActive(false);
                return;
            }
            
            gameObject.SetActive(true);
            
            _transform.localPosition = _transformPositionDefault.WithY(Mathf.Lerp(_transformPositionDefault.y, _transformPositionDefault.y + _transformOffsetY, t));
            _renderer.material.SetVector(PositionOffset, _materialOffsetDefault.WithY(Mathf.Lerp(_materialOffsetYStart, _materialOffsetYEnd, t)));
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (Application.isPlaying && _transform != null && _renderer != null) ApplyProgress(_progress);
        }
#endif
    }
    
}