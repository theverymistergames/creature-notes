using System;
using MisterGames.Actors;
using MisterGames.Character.View;
using MisterGames.Common.Lists;
using MisterGames.Common.Maths;
using MisterGames.Tick.Core;
using UnityEngine;

namespace _Project.Scripts.Runtime.Fireball {
    
    
    public class FireballShaderController : MonoBehaviour, IActorComponent, IUpdate {

        [Header("Detection")]
        [SerializeField] [Min(1)] private int _speedBufferSize = 5;
        [SerializeField] [Min(1)] private int _angleBufferSize = 5;
        [SerializeField] private float _speedMul = 1f;
        [SerializeField] private float _minSpeed = 0.1f;
        [SerializeField] private float _maxSpeed = 1f;
        [SerializeField] private AnimationCurve _speedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [Header("Material Settings")]
        [SerializeField] private Material _material;
        [SerializeField] private string _rotationParameter;
        [SerializeField] private string _blendParameter;
        [SerializeField] private float _angleSmoothing = 10f;
        [SerializeField] private float _blendSmoothing = 10f;
        
        private CharacterViewPipeline _view;
        private float _lastUpdateTime;

        private Vector3[] _deltaBuffer;
        private float[] _speedBuffer;
        private int _speedPointer;
        private int _anglePointer;
        private Vector3 _lastPoint;
        private float _lastAngle;

        private Quaternion _rotSmoothed = Quaternion.identity;
        private float _angleSmoothed;
        private float _blendSmoothed;

        private int _rotationParameterHash;
        private int _blendParameterHash;
        
        void IActorComponent.OnAwake(IActor actor) {
            _speedBuffer = new float[_speedBufferSize];
            _deltaBuffer = new Vector3[_angleBufferSize];
            
            _view = actor.GetComponent<CharacterViewPipeline>();
            _lastPoint = _view.Orientation * Vector3.forward;

            _rotationParameterHash = Shader.PropertyToID(_rotationParameter);
            _blendParameterHash = Shader.PropertyToID(_blendParameter);
        }

        private void OnEnable() {
            PlayerLoopStage.LateUpdate.Subscribe(this);
        }

        private void OnDisable() {
            PlayerLoopStage.LateUpdate.Unsubscribe(this);
        }

        void IUpdate.OnUpdate(float dt) {
            var orient = _view.Orientation;
            var point = orient * Vector3.forward;
            var delta = Quaternion.Inverse(orient) * (point - _lastPoint);
            _lastPoint = point;

            float speed = dt > 0f ? delta.magnitude * _speedMul / dt : 0f;
            float speedAvg = _speedBuffer.WriteToCircularBufferAndGetAverage(speed, ref _speedPointer);

            var writeDelta = speedAvg > _minSpeed ? delta : Vector3.zero;
            var deltaAvg = _deltaBuffer.WriteToCircularBufferAndGetAverage(writeDelta, ref _anglePointer);
            float angleAvg = Vector3.SignedAngle(Vector3.right, deltaAvg, Vector3.forward);
            _lastAngle = speedAvg > _minSpeed ? angleAvg : _lastAngle;
            
            _angleSmoothed = Mathf.Lerp(VectorUtils.GetNearestAngle(_angleSmoothed, _lastAngle), _lastAngle, dt * _angleSmoothing);
            
            //_rotSmoothed = Quaternion.Slerp(_rotSmoothed, Quaternion.Euler(angleAvg, 0f, 0f), dt * _angleSmoothing);
            
            float relativeSpeed = _speedCurve.Evaluate(Mathf.Clamp01(Mathf.Max(speedAvg - _minSpeed, 0f) / (_maxSpeed - _minSpeed)));

            float blend = relativeSpeed;
            _blendSmoothed = Mathf.Lerp(_blendSmoothed, blend, dt * _blendSmoothing);

            float angle = _angleSmoothed - 180f;
            ApplyParameters(_blendSmoothed, angle);
            
            Debug.Log($"FireballShaderController.OnUpdate: speed {speedAvg:0.000}, angle {angle:0.000}, blend {_blendSmoothed:0.000}");
        }

        private void ApplyParameters(float blend, float angle) {
            //_material.SetFloat(_blendParameterHash, blend);
            _material.SetFloat(_rotationParameterHash, angle);
        }
        
        private void OnValidate() {
            if (!Application.isPlaying) return;

            if (_speedBuffer?.Length != _speedBufferSize) {
                _speedBuffer = new float[_speedBufferSize];
                _speedPointer = 0;
            }

            if (_deltaBuffer?.Length != _angleBufferSize) {
                _deltaBuffer = new Vector3[_angleBufferSize];
                _anglePointer = 0;
            }
        }
    }
    
}