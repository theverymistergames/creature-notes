using System;
using MisterGames.Character.Core;
using MisterGames.Common;
using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Common.Tick;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MisterGames.Logic.Transforms {
    
    public sealed class EyesMotion : MonoBehaviour, IUpdate {

        [SerializeField] private Transform _root;
        [SerializeField] private Transform[] _eyes;
        
        [Header("Rotation")]
        [SerializeField] private bool _preserveOriginRotationOffset;
        [SerializeField] [Min(0f)] private float _smoothing = 5f;
        [SerializeField] [Range(-180f, 180f)] private float _minAngleHorizontal;
        [SerializeField] [Range(-180f, 180f)] private float _maxAngleHorizontal;
        [SerializeField] [Range(-180f, 180f)] private float _minAngleVertical;
        [SerializeField] [Range(-180f, 180f)] private float _maxAngleVertical;

        [Header("Random")]
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _changeDirectionTime;
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _pointDistance;
        [SerializeField] [Range(0f, 1f)] private float _separateRandomDirection;

        private enum LookAtMode {
            None,
            Point,
            Transform,
        }
        
        private Quaternion[] _rotationOffsets;
        private Vector3[] _lookDirs;
        private float[] _nextDirectionChangeTimes;
        private LookAtMode _lookAtMode;
        private Transform _target;
        private Vector3 _targetPoint;

        private void Awake() {
            _rotationOffsets = new Quaternion[_eyes.Length];
            _lookDirs = new Vector3[_eyes.Length];
            _nextDirectionChangeTimes = new float[_eyes.Length];

            var forward = _root.forward;
            var point = Vector3.forward * _pointDistance.GetRandomInRange();
            
            for (int i = 0; i < _rotationOffsets.Length; i++) {
                _rotationOffsets[i] = Quaternion.FromToRotation(forward, _eyes[i].forward);
            }
            
            for (int i = 0; i < _lookDirs.Length; i++) {
                _lookDirs[i] = point;
            }
        }

        private void OnEnable() {
            PlayerLoopStage.LateUpdate.Subscribe(this);
            
            LookAt(CharacterSystem.Instance.GetCharacter().Transform);
        }

        private void OnDisable() {
            PlayerLoopStage.LateUpdate.Unsubscribe(this);
        }

        public void LookAt(Transform target) {
            _lookAtMode = target == null ? LookAtMode.None : LookAtMode.Transform;
            _target = target;
        }

        public void LookAt(Vector3 point) {
            _lookAtMode = LookAtMode.Point;
            _targetPoint = point;
            _target = null;
        }

        public void StopLookAt() {
            _lookAtMode = LookAtMode.None;
            _target = null;
        }
        
        void IUpdate.OnUpdate(float dt) {
            ProcessRandomChanges();
            ProcessRotation(dt);
        }

        private void ProcessRandomChanges() {
            float time = Time.time;
            bool changedOne = false;
            
            for (int i = 0; i < _nextDirectionChangeTimes.Length; i++) {
                if (time < _nextDirectionChangeTimes[i]) continue;
                
                _nextDirectionChangeTimes[i] = time + _changeDirectionTime.GetRandomInRange();
                _lookDirs[i] = GetRandomDir();
                
                changedOne = true;
            }

            if (!changedOne || Random.value < _separateRandomDirection) return;
            
            float nextTimeTogether = time + _changeDirectionTime.GetRandomInRange();
            var nextDir = GetRandomDir();
                
            for (int i = 0; i < _nextDirectionChangeTimes.Length; i++) { 
                _nextDirectionChangeTimes[i] = nextTimeTogether;
                _lookDirs[i] = nextDir;
            }
        }
        
        private void ProcessRotation(float dt) {
            _root.GetPositionAndRotation(out var pos, out var rot);
            
            for (int i = 0; i < _eyes.Length; i++) {
                var eye = _eyes[i];
                
                var point = _lookAtMode switch {
                    LookAtMode.None => pos + rot * _lookDirs[i],
                    LookAtMode.Point => ApplyLimits(_targetPoint),
                    LookAtMode.Transform => ApplyLimits(_target.position),
                    _ => throw new ArgumentOutOfRangeException()
                };

#if UNITY_EDITOR
                if (_showDebugInfo) DebugExt.DrawLine(eye.position, point, Color.magenta);
                if (_showDebugInfo) DebugExt.DrawSphere(point, 0.03f, Color.magenta);
#endif
                
                var targetRot = Quaternion.LookRotation(point - eye.position, rot * Vector3.up) * 
                                (_preserveOriginRotationOffset ? _rotationOffsets[i] : Quaternion.identity);
                
                eye.rotation = eye.rotation.SlerpNonZero(targetRot, _smoothing, dt);
            }
        }

        private Vector3 ApplyLimits(Vector3 point) {
            _root.GetPositionAndRotation(out var pos, out var rot);
            
            var dir = point - pos;
            var forward = rot * Vector3.forward;
            
            var rotOffset = Quaternion.Inverse(rot) * Quaternion.LookRotation(dir, rot * Vector3.up);
            var angles = rotOffset.ToEulerAngles180();
            
            angles.x = Mathf.Clamp(angles.x, _minAngleVertical, _maxAngleVertical);
            angles.y = Mathf.Clamp(angles.y, _minAngleHorizontal, _maxAngleHorizontal);
            
            return pos + Quaternion.FromToRotation(dir, Quaternion.Euler(-angles.x, angles.y, 0f) * forward) * dir;
        }

        private Vector3 GetRandomDir() {
            var rot = Quaternion.Euler(
                Random.Range(_minAngleVertical, _maxAngleVertical),
                Random.Range(_minAngleHorizontal, _maxAngleHorizontal),
                0f
            );
            
            return rot * (_pointDistance.GetRandomInRange() * Vector3.forward);
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo;

        private void OnDrawGizmos() {
            if (!_showDebugInfo || _root == null) return;

            var pos = _root.position;
            var rot = _root.rotation;
            
            var dir00 = Quaternion.Euler(_minAngleVertical, _minAngleHorizontal, 0f) * Vector3.forward;
            var dir01 = Quaternion.Euler(_minAngleVertical, _maxAngleHorizontal, 0f) * Vector3.forward;
            var dir10 = Quaternion.Euler(_maxAngleVertical, _minAngleHorizontal, 0f) * Vector3.forward;
            var dir11 = Quaternion.Euler(_maxAngleVertical, _maxAngleHorizontal, 0f) * Vector3.forward;

            var p00Close = pos + rot * dir00 * _pointDistance.x;
            var p00Far = pos + rot * dir00 * _pointDistance.y;
            
            var p01Close = pos + rot * dir01 * _pointDistance.x;
            var p01Far = pos + rot * dir01 * _pointDistance.y;
            
            var p10Close = pos + rot * dir10 * _pointDistance.x;
            var p10Far = pos + rot * dir10 * _pointDistance.y;
            
            var p11Close = pos + rot * dir11 * _pointDistance.x;
            var p11Far = pos + rot * dir11 * _pointDistance.y;
            
            DebugExt.DrawLine(p00Close, p00Far, Color.cyan, gizmo: true);
            DebugExt.DrawLine(p01Close, p01Far, Color.cyan, gizmo: true);
            DebugExt.DrawLine(p10Close, p10Far, Color.cyan, gizmo: true);
            DebugExt.DrawLine(p11Close, p11Far, Color.cyan, gizmo: true);
            
            DebugExt.DrawLine(p00Close, p01Close, Color.cyan, gizmo: true);
            DebugExt.DrawLine(p00Close, p10Close, Color.cyan, gizmo: true);
            DebugExt.DrawLine(p11Close, p01Close, Color.cyan, gizmo: true);
            DebugExt.DrawLine(p11Close, p10Close, Color.cyan, gizmo: true);
            
            DebugExt.DrawLine(p00Far, p01Far, Color.cyan, gizmo: true);
            DebugExt.DrawLine(p00Far, p10Far, Color.cyan, gizmo: true);
            DebugExt.DrawLine(p11Far, p01Far, Color.cyan, gizmo: true);
            DebugExt.DrawLine(p11Far, p10Far, Color.cyan, gizmo: true);
        }
#endif
    }
    
}