using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.Common.Lists;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;

public class ActivateGameObjectOnLevelsLoaded : MonoBehaviour, IEventListener {
    [SerializeField] private EventReference levelLoadedEvent;
    [SerializeField] private List<int> levels;
    [SerializeField] private GameObject go;
    [SerializeField] private bool _emissive;
    
    private float _startIntensity;
    private Color _startColor;
    private Material _material;
    private static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");
    private static readonly int EmissiveIntensity = Shader.PropertyToID("_EmissiveIntensity");

    private void Awake() {
        if (!go) go = gameObject;
        
        if (_emissive) {
            _material = go.GetComponent<MeshRenderer>().materials[0];
            _startIntensity = _material.GetFloat(EmissiveIntensity);
            _startColor = _material.GetColor(EmissiveColor);
        }
    }

    public void OnEventRaised(EventReference e) {
        if (e.EventId == levelLoadedEvent.EventId) {
            SetActiveIfNeeded();
        }
    }

    private void SetActiveIfNeeded() {
        var count = levelLoadedEvent.GetCount();
        
        if (levels.Contains(count)) {
            if (_emissive) {
                _material.SetColor(EmissiveColor, _startColor * _startIntensity);
            } else {
                go.SetActive(true);
            }
        } else {
            if (_emissive) { 
                _material.SetColor(EmissiveColor, Color.black);
            } else {
                go.SetActive(false); 
            }
        }
    }

    private void OnEnable() {
        SetActiveIfNeeded();
        levelLoadedEvent.Subscribe(this);
    }

    private void OnDisable() {
        levelLoadedEvent.Unsubscribe(this);
    }
}
