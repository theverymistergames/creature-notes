using System;
using System.Collections;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightMonsterAnimation : MonsterAnimation {
    [SerializeField] private GameObject lightSource;
    [SerializeField] private MeshRenderer emissiveObject;

    private AudioSource _audio;
    
    private float _targetTime;
    private float _progress;
    private Material _material;
    private float _startEmissionIntensity;
    private static readonly int EmissionIntensity = Shader.PropertyToID("_EmissiveIntensity");

    private Color _color;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissiveColor");

    private void Start() {
        SubscribeUpdate();

        _audio = GetComponent<AudioSource>();
        monster.SetActive(false);

        _material = emissiveObject.materials[0];
        _color = _material.GetColor(EmissionColor);
        _startEmissionIntensity = _material.GetFloat(EmissionIntensity);
    }

    protected override void ProceedUpdate(float progress) {
        _progress = progress;
        
        if (progress == 0) {
            SetMonsterVisible(false);
        } else if (progress > .99f) {
            StopCoroutine(BlinkRoutine(progress));
            SetMonsterVisible(true);
            return;
        }
        
        _targetTime -= Time.deltaTime;

        if (_targetTime > 0) return;
        
        _targetTime = Random.Range(.2f, .5f) * (1 - progress / 2) + 5 * (1 - progress);

        StartCoroutine(BlinkRoutine(progress));
    }

    private void SetMonsterVisible(bool visible) {
        lightSource.SetActive(!visible);
        monster.SetActive(visible && _progress >= harbingerThreshold);
        _material.SetColor(EmissionColor, (visible ? Color.black : _color) * _startEmissionIntensity);
    }

    private IEnumerator BlinkRoutine(float progress) {
        SetMonsterVisible(true);
        
        yield return new WaitForSeconds(0.2f - 0.1f * progress);
        
        _audio.Play();

        SetMonsterVisible(false);
    }
}
