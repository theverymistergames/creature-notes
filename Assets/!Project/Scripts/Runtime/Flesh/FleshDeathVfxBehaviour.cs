using MisterGames.Character.Core;
using MisterGames.Common.Easing;
using MisterGames.Common.Tick;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {

    public sealed class FleshDeathVfxBehaviour : MonoBehaviour, IUpdate {

        [Header("Hands")]
        [SerializeField] private Transform _hands;
        [SerializeField] private Vector2 _fovRange;
        [SerializeField] private Vector2 _handsOffsetRange;
        [SerializeField] private AnimationCurve _handsOffsetCurve = EasingType.Linear.ToAnimationCurve();
        [SerializeField] private float _handsAngularSpeed;
        [SerializeField] private bool _randomRotation;
        
        private Camera _camera;
        
        private void OnEnable() {
            _camera = CharacterSystem.Instance.GetCharacter().TryGetComponent(out Camera camera) ? camera : null;
            
            if (_randomRotation) _hands.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            
            PlayerLoopStage.LateUpdate.Subscribe(this);
        }

        private void OnDisable() {
            PlayerLoopStage.LateUpdate.Unsubscribe(this);
        }

        void IUpdate.OnUpdate(float dt) {
            _hands.GetLocalPositionAndRotation(out var pos, out var rot);
            
            float fovT = (_camera.fieldOfView - _fovRange.x) / (_fovRange.y - _fovRange.x);
            pos.z = Mathf.Lerp(_handsOffsetRange.x, _handsOffsetRange.y, _handsOffsetCurve.Evaluate(fovT));
            
            rot *= Quaternion.Euler(0f, 0f, _handsAngularSpeed * dt);
            
            _hands.SetLocalPositionAndRotation(pos, rot);
        }
    }
    
}