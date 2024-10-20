using System;
using MisterGames.Common.Attributes;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class Meteor : MonoBehaviour {
    [SerializeField] private DecalProjector projector;
    [SerializeField] private Light _light;
    
    private ParticleSystem _system;
    private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[5];
    
    private bool _collided;
    private bool _animInProgress;
    private float _startDistance;

    private void Awake() {
        _system = GetComponent<ParticleSystem>();
        projector.fadeFactor = 0.1f;
    }

    [Button]
    public void Animate() {
        _collided = false;
        
        projector.fadeFactor = 0.1f;
        
        _system = GetComponent<ParticleSystem>();
        _system.Play();
        _system.GetParticles(_particles);
        
        _startDistance = Vector3.Distance(projector.transform.position, _system.transform.TransformPoint(_particles[0].position));

        _animInProgress = true;
    }

    private void OnParticleCollision(GameObject other) {
        if (!_collided) {
            _collided = true;
            _animInProgress = false;
            
            OnCollision();
        }
    }

    private void Update() {
        _system.GetParticles(_particles);
        
        if (_animInProgress) {
            var dist = Vector3.Distance(projector.transform.position, _system.transform.TransformPoint(_particles[0].position));
            
            projector.fadeFactor = Math.Min(1, Math.Max(0.1f, (3 - dist) / 3)) / 1;
        }
    }

    void OnCollision() {
        
    }
}
