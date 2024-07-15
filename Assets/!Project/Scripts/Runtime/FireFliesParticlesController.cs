using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using MisterGames.Common.Attributes;
using UnityEngine;

public class FireFliesParticlesController : MonoBehaviour {
    [SerializeField] private float duration = 3f;
    
    private ParticleSystem _system;
    private ParticleSystem.MinMaxCurve _startRate;
    
    void Awake() {
        _system = GetComponent<ParticleSystem>();
        _startRate = _system.emission.rateOverTime;
    }

    public void Reset() {
        var main = _system.main;
        var emission = _system.emission;
        
        main.gravityModifier = 0;
        emission.rateOverTime = _startRate;
        
        _system.Play();
    }

    public void Open() {
        var main = _system.main;
        var emission = _system.emission;
        var rate = emission.rateOverTime;

        LMotion.Create(0f, 1f, duration).WithOnComplete(() => {
            _system.Stop();
        }).Bind(value => {
            main.gravityModifier = 0 - value;
            emission.rateOverTime = rate.constant * (1 - value);
        });
    }
}
