using MisterGames.Actors;
using MisterGames.Character.View;
using MisterGames.Common.Lists;
using MisterGames.Common.Maths;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace _Project.Scripts.Runtime.Fireball {
    
    public sealed class FireballShaderController : MonoBehaviour, IActorComponent, IUpdate {

        [Header("Material")]
        [SerializeField] private CustomPassVolume _customPassVolume;
        [SerializeField] private Material _material;
        [SerializeField] private string _centerOffsetProperty;
        
        [Header("Center Offset")]
        [SerializeField] [Min(1)] private int _bufferSize = 5;
        [SerializeField] private float _minSpeed = 0.1f;
        [SerializeField] private float _maxSpeed = 1f;
        [SerializeField] private float _speedMul = 1f;
        [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] [Min(0f)] private float _maxOffset;
        [SerializeField] [Min(0f)] private float _offsetSmoothing = 5f;

        private Material _runtimeMaterial;
        
        private CharacterViewPipeline _view;
        private Vector2[] _deltaBuffer;
        private float[] _speedBuffer;
        private int _speedPointer;
        private int _deltaPointer;
        
        private Vector3 _lastPoint;
        private Vector2 _centerOffsetSmoothed;
        private int _centerOffsetHash;
        
        void IActorComponent.OnAwake(IActor actor) {
            InstantiateRuntimeMaterial();
            
            _speedBuffer = new float[_bufferSize];
            _deltaBuffer = new Vector2[_bufferSize];
            
            _view = actor.GetComponent<CharacterViewPipeline>();
            _centerOffsetHash = Shader.PropertyToID(_centerOffsetProperty);
        }

        private void InstantiateRuntimeMaterial() {
            _runtimeMaterial = new Material(_material);
            
            ((FullScreenCustomPass) _customPassVolume.customPasses[0]).fullscreenPassMaterial = _runtimeMaterial;
        }

        private void OnEnable() {
            _lastPoint = _view.Orientation * Vector3.forward;
            _centerOffsetSmoothed = Vector2.zero;
            
            PlayerLoopStage.LateUpdate.Subscribe(this);
        }

        private void OnDisable() {
            PlayerLoopStage.LateUpdate.Unsubscribe(this);
        }

        void IUpdate.OnUpdate(float dt) {
            ProcessCenterOffset(dt);
        }

        private void ProcessCenterOffset(float dt) {
            var orient = _view.Orientation;
            var point = orient * Vector3.forward;
            var delta = Quaternion.Inverse(orient) * (point - _lastPoint);
            _lastPoint = point;

            float speed = dt > 0f ? delta.magnitude / dt : 0f;
            float speedAvg = _speedBuffer.WriteToCircularBufferAndGetAverage(speed, ref _speedPointer);
            float relativeSpeed = Mathf.Clamp01(Mathf.Max(speedAvg - _minSpeed, 0f) / (_maxSpeed - _minSpeed));

            var deltaAvg = _deltaBuffer.WriteToCircularBufferAndGetAverage(delta, ref _deltaPointer);
            var centerOffset = deltaAvg.normalized * Mathf.Min(_speedCurve.Evaluate(relativeSpeed) * _speedMul, _maxOffset);
            _centerOffsetSmoothed = _centerOffsetSmoothed.SmoothExp(centerOffset, dt * _offsetSmoothing);
            
            _runtimeMaterial.SetVector(_centerOffsetHash, _centerOffsetSmoothed);
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!Application.isPlaying) return;

            if (_speedBuffer?.Length != _bufferSize) {
                _speedBuffer = new float[_bufferSize];
                _speedPointer = 0;
            }

            if (_deltaBuffer?.Length != _bufferSize) {
                _deltaBuffer = new Vector2[_bufferSize];
                _deltaPointer = 0;
            }
        }
#endif
    }
    
}