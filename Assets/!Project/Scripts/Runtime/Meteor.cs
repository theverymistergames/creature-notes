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

    private void Awake() {
        _system = GetComponent<ParticleSystem>();
        projector.fadeFactor = 0;
    }

    [Button]
    public void Animate() {
        _animInProgress = true;
        
        // _system.Play();
        Debug.Log("DLWKdwddwdddcvdzvwsdcccdwdwc ccccKDdwdw");
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
            // Debug.Log(Vector3.Distance(projector.transform.position, _particles[0].position + _system.transform.position));
            projector.fadeFactor = Math.Max(1, Vector3.Distance(projector.transform.position, _particles[0].position));
        }
    }

    void OnCollision() {
        
    }
}
