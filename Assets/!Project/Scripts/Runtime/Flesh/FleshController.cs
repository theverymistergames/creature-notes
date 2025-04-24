using MisterGames.Common.Easing;
using MisterGames.Common.Maths;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshController : MonoBehaviour {

        [SerializeField] private Renderer _renderer;
        [SerializeField] private float _transformOffsetY = 2f;
        [SerializeField] private float _materialOffsetYStart = 0f;
        [SerializeField] private float _materialOffsetYEnd = 2f;
        [SerializeField] private AnimationCurve _curve = EasingType.Linear.ToAnimationCurve();
        [SerializeField] [Range(0f, 1f)] private float _progress;
        [SerializeField] [Range(0f, 1f)] private float _enableAtProgress;

        private static readonly int PositionOffset = Shader.PropertyToID("_Position_Offset");
        
        public float Progress => _progress;

        private Vector3 _materialOffsetDefault;
        
        private void Awake() {
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

            float materialOffset = Mathf.Lerp(_materialOffsetYStart, _materialOffsetYEnd, t);
            
            _renderer.material.SetVector(PositionOffset, _materialOffsetDefault.WithY(materialOffset));

            var bounds = _renderer.localBounds;
            bounds.size = bounds.size.WithY(materialOffset * 2f);
            _renderer.localBounds = bounds;
        }
    }
    
}