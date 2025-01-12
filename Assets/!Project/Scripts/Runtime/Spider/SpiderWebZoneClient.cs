using System.Collections.Generic;
using MisterGames.Actors;
using MisterGames.Collisions.Rigidbodies;
using MisterGames.Common.Attributes;
using MisterGames.Common.Labels;
using MisterGames.Common.Maths;
using MisterGames.Common.Tick;
using MisterGames.Logic.Phys;
using UnityEngine;

namespace _Project.Scripts.Runtime.Spider {
    
    public sealed class SpiderWebZoneClient : MonoBehaviour, IActorComponent, IUpdate {
        
        [SerializeField] private TriggerEmitter _triggerEmitter;
        [SerializeField] [Min(0)] private int _maxWebColliders = 10;
        [SerializeField] [MinMaxSlider(0f, 1f)] private Vector2 _slowFactorRange = new Vector2(0f, 1f);
        [SerializeField] [Min(0f)] private float _slowFactorSmoothing = 1f;
        [SerializeField] private AnimationCurve _slowCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private bool _disableGravity = true;
        [VisibleIf(nameof(_disableGravity))]
        [SerializeField] private LabelValue _gravityPriority;
        
        private readonly HashSet<Collider> _colliders = new();
        private RigidbodyPriorityData _rigidbodyData;
        private float _slowFactorSmoothed;

        void IActorComponent.OnAwake(IActor actor) {
            _rigidbodyData = actor.GetComponent<RigidbodyPriorityData>();
        }

        private void OnEnable() {
            _triggerEmitter.TriggerEnter += TriggerEnter;
            _triggerEmitter.TriggerExit += TriggerExit;
        }

        private void OnDisable() {
            _triggerEmitter.TriggerEnter -= TriggerEnter;
            _triggerEmitter.TriggerExit -= TriggerExit;
            
            PlayerLoopStage.FixedUpdate.Unsubscribe(this);
            _colliders.Clear();
            
            if (_disableGravity) _rigidbodyData.RemoveUseGravity(this);
        }

        private void TriggerEnter(Collider collider) {
            _colliders.Add(collider);
            PlayerLoopStage.FixedUpdate.Subscribe(this);

            if (_disableGravity) {
                _rigidbodyData.SetUseGravity(this, useGravity: false, _gravityPriority.GetValue());
            }
        }

        private void TriggerExit(Collider collider) {
            _colliders.Remove(collider);
            
            if (_colliders.Count > 0) return;
            
            PlayerLoopStage.FixedUpdate.Unsubscribe(this);
            if (_disableGravity) _rigidbodyData.RemoveUseGravity(this);
        }

        void IUpdate.OnUpdate(float dt) {
            _colliders.RemoveWhere(c => c == null || !c.enabled);
            
            float slowFactorTarget = GetTargetSlowFactor(_colliders.Count);
            float smoothing = slowFactorTarget > 0f ? _slowFactorSmoothing : 0f;
            _slowFactorSmoothed = _slowFactorSmoothed.SmoothExpNonZero(slowFactorTarget, dt * smoothing);
            
            var force = dt > 0f ? _rigidbodyData.Rigidbody.linearVelocity / dt : Vector3.zero;
            _rigidbodyData.Rigidbody.AddForce(-force * _slowFactorSmoothed, ForceMode.Acceleration);

            if (_colliders.Count > 0) return;
            
            PlayerLoopStage.FixedUpdate.Unsubscribe(this);
            if (_disableGravity) _rigidbodyData.RemoveUseGravity(this);
        }

        private float GetTargetSlowFactor(int colliders) {
            if (_maxWebColliders <= 0) return 0f;
            
            float t = Mathf.Clamp01((float) colliders / _maxWebColliders);
            return Mathf.Lerp(_slowFactorRange.x, _slowFactorRange.y, _slowCurve.Evaluate(t));
        }
    }
    
}