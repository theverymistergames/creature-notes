using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Easing;
using MisterGames.Common.Maths;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Enemies.Bed {
    
    public sealed class GravityAttackBehaviour : MonoBehaviour {

        [SerializeField] private Transform _gravitySource;

        [Header("Flip")]
        [SerializeField] [Range(0f, 90f)] private float _flipAngleMax = 45f; 
        [SerializeField] private Vector2 _flipMagnitude = Vector2.one;
        [SerializeField] [Min(0f)] private float _flipDuration = 0.2f;
        
        [Header("Settle")]
        [SerializeField] private Vector2 _settleMagnitude = Vector2.one;
        [SerializeField] [Min(0f)] private float _settleDuration = 0.2f;
        
        [Header("Rotation")]
        [SerializeField] [Min(0f)] private float _rotationMagnitude = 0.001f;
        [SerializeField] [Min(0f)] private float _rotationSpeedStart = 1f;
        [SerializeField] [Min(0f)] private float _rotationSpeedEnd = 1f;
        [SerializeField] [Min(0f)] private float _rotationDuration = 3f;
        [SerializeField] private AnimationCurve _rotationCurve = EasingType.Linear.ToAnimationCurve();

        [Header("Stop")]
        [SerializeField] private Vector2 _stopMagnitude = Vector2.one;
        
        private CancellationTokenSource _enableCts;
        private Vector3 _initialForward;
        private byte _operationId;

        private void Awake() {
            _initialForward = _gravitySource.forward;
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            
            _gravitySource.forward = _initialForward;
            _gravitySource.localScale = Vector3.one;
        }
        
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        public void StartAntiGravity() {
            StartAntiGravityAsync(_enableCts.Token).Forget();
        }

        [Button(mode: ButtonAttribute.Mode.Runtime)]
        public void StopAntiGravity() {
            byte id = ++_operationId;
            
            float stopMagnitude = _stopMagnitude.GetRandomInRange();
            _gravitySource.localScale = Vector3.one.WithZ(stopMagnitude);
        }

        private async UniTask StartAntiGravityAsync(CancellationToken cancellationToken) {
            byte id = ++_operationId;
            
            var originDir = _gravitySource.forward;
            var up = -Physics.gravity.normalized;

            float flipMagnitude = _flipMagnitude.GetRandomInRange();
            float angle = Random.Range(0f, _flipAngleMax);
            
            var flatDir = RandomExtensions.OnUnitCircle(up);
            var axis = Vector3.Cross(flatDir, up);
            var flipDir = Quaternion.AngleAxis(angle, axis) * flatDir;
            
            if (_flipDuration > 0f) {
                _gravitySource.forward = flipDir;
                _gravitySource.localScale = Vector3.one.WithZ(flipMagnitude);
            
                await UniTask.Delay(TimeSpan.FromSeconds(_flipDuration), cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();
            
                if (cancellationToken.IsCancellationRequested || _operationId != id) return;   
            }

            if (_settleDuration > 0f) {
                float settleMagnitude = _settleMagnitude.GetRandomInRange();
            
                _gravitySource.forward = originDir;
                _gravitySource.localScale = Vector3.one.WithZ(settleMagnitude);
            
                await UniTask.Delay(TimeSpan.FromSeconds(_settleDuration), cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();
            
                if (cancellationToken.IsCancellationRequested || _operationId != id) return;   
            }
            
            var startRot = _gravitySource.rotation;
            var endRot = _gravitySource.rotation * Quaternion.FromToRotation(originDir, flipDir);
            float t = 0f;
            float s = _rotationDuration > 0f ? 1f / _rotationDuration : float.MaxValue;
            float tRot = 0f;

            while (!cancellationToken.IsCancellationRequested && _operationId == id && t < 1f) {
                float dt = Time.deltaTime;
                t += dt * s;
                
                float speed = Mathf.Lerp(_rotationSpeedStart, _rotationSpeedEnd, _rotationCurve.Evaluate(t));
                tRot += dt * speed;

                _gravitySource.rotation = Quaternion.SlerpUnclamped(startRot, endRot, tRot);
                _gravitySource.localScale = Vector3.one.WithZ(_rotationMagnitude);
                
                await UniTask.Yield();
            }
        }

    }
    
}