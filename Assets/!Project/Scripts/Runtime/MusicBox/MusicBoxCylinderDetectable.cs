using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using MisterGames.Interact.Detectables;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class MusicBoxCylinderDetectable : MonoBehaviour {
    [SerializeField] private HDAdditionalLightData lightObj;
    [SerializeField] private GameObject cylinderGO;
    [SerializeField] private GameObject placeGO;
    [SerializeField] private GameObject paintingGO;
    [SerializeField] private ParticleSystem particles;
    private Detectable _detectable;
    private float _startIntensity;
    private MotionHandle _currentTween;

    private void Start() {
        _detectable = GetComponent<Detectable>();
        _detectable.OnDetectedBy += OnDetected;
        _detectable.OnLostBy += OnLost;
        
        _startIntensity = lightObj.intensity;
        lightObj.intensity = 0;
    }

    private void OnLost(IDetector obj) {
        if (_currentTween.IsActive()) _currentTween.Cancel();

        var intensity = lightObj.intensity;
        
        _currentTween = LMotion.Create(0f, 1f, .2f).Bind((t) => {
            lightObj.intensity = Mathf.Lerp(intensity, 0, t);
        });
    }

    private void OnDetected(IDetector obj) {
        if (_currentTween.IsActive()) _currentTween.Cancel();
        
        var intensity = lightObj.intensity;
        
        _currentTween = LMotion.Create(0f, 1f, 3f).WithOnComplete(() => {
            cylinderGO.SetActive(false);
            paintingGO.SetActive(false);
            placeGO.SetActive(true);
            particles.Play();
        }).Bind(t => {
            lightObj.intensity = Mathf.Lerp(intensity, _startIntensity, t);
        });
    }
}
