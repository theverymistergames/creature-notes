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
    
    void Awake() {
        _source = GetComponent<AudioSource>();
        _totalDuration = _source.clip.length;
        _timeStep = _totalDuration / steps;

        SetStep(startStep, true);
    }

    public void Increase() {
       SetStep(_currentStep + 1);
    }
    
    public void Decrease() {
        SetStep(_currentStep - 1);
    }

    public void SetStep(int step, bool instant = false) {
        _currentStep = step;
        _source.time = _timeStep * GetRealStep();

        var rot = transform.localRotation;
        
        if (_currentTween.IsActive()) _currentTween.Cancel();

        if (gameObject.activeInHierarchy) {
            _currentTween = LMotion.Create(0f, 1f, instant ? 0f : 0.2f).Bind(value => {
                transform.localRotation = Quaternion.Lerp(rot, Quaternion.Euler(new Vector3(0, 360, 0) * _currentStep / steps), value);
            });   
        }
    }

    int GetRealStep() {
        return (_currentStep % steps + steps) % steps;
    }

    public void Play() {
        if (!gameObject.activeInHierarchy) return;
        
        _source.time = _timeStep * GetRealStep();
        _source.Play();
    }

    public void Stop() {
        if (!gameObject.activeInHierarchy) return;
        
        _source.Stop();
    }
}
