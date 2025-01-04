using Deform;
using MisterGames.Common.Maths;
using MisterGames.Common.Tick;
using UnityEngine;

namespace MisterGames.Character.Transport {
    
    public sealed class CarAntennaPhysics : MonoBehaviour, IUpdate {

        [SerializeField] private Transform _endPoint;
        [SerializeField] private BendDeformer _bendDeformer;
        [SerializeField] private Vector3 _rotationOffset;
        [SerializeField] [Range(0f, 90f)] private float _maxAngle = 90f;
        [SerializeField] private float _angleMultiplier = 1f;
        [SerializeField] private AnimationCurve _angleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        private Transform _axis;
        private Transform _transform;

        private void Awake() {
            _transform = transform;
            _axis = _bendDeformer.Axis;
        }

        private void OnEnable() {
            PlayerLoopStage.Update.Subscribe(this);
        }

        private void OnDisable() {
            PlayerLoopStage.Update.Unsubscribe(this);
        }

        public void OnUpdate(float dt) {
            var up = _transform.up;
            var diff = _endPoint.position - _axis.position;
            var diffProj = Vector3.ProjectOnPlane(diff, up);

            var rotOffset = Quaternion.Euler(_rotationOffset);
            
            if (diffProj != Vector3.zero) {
                _axis.rotation = Quaternion.LookRotation(diffProj, up) * rotOffset;
            }

            float angle = Vector3.SignedAngle(up, diff, rotOffset * _axis.right);
            float resultAngle = Mathf.Sign(angle) * _angleCurve.Evaluate(Mathf.Abs(angle) / _maxAngle) * _maxAngle * _angleMultiplier;

            _bendDeformer.Angle = resultAngle;
        }
    }
    
}