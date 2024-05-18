using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;

public class MusicBoxPart : MonoBehaviour {
    [SerializeField] private int steps = 16;
    [SerializeField] private int startStep = 0;

    private int _currentStep = 0;
    private AudioSource _source;
    private float _totalDuration;
    private float _timeStep;
    private bool _playing;
    private float _timer;

    private MotionHandle _currentTween;
    
    void Start() {
        _source = GetComponent<AudioSource>();
        _totalDuration = _source.clip.length;
        _timeStep = _totalDuration / steps;

        SetStep(startStep);
    }

    public void Increase() {
       SetStep(_currentStep + 1);
    }
    
    public void Decrease() {
        SetStep(_currentStep - 1);
    }

    public void SetStep(int step) {
        if (step < 0) step = steps - 1;
        step %= steps;
        
        _currentStep = step % steps;
        _source.time = _timeStep * _currentStep;

        var angles = transform.localEulerAngles;
        
        if (_currentTween.IsActive()) _currentTween.Cancel();

        if (gameObject.activeSelf) {
            _currentTween = LMotion.Create(0f, 1f, 0.2f).Bind(value => {
                transform.localEulerAngles = Vector3.Lerp(angles, new Vector3(0, 360, 0) * _currentStep / steps, value);
            });   
        }
    }

    public void Play() {
        if (!gameObject.activeSelf) return;
        
        _source.time = _timeStep * _currentStep;
        _source.Play();
    }

    public void Stop() {
        if (!gameObject.activeSelf) return;
        
        _source.Stop();
    }
}
