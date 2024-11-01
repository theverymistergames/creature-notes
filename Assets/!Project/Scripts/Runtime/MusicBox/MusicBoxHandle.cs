using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MisterGames.BlueprintLib;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;

public class MusicBoxHandle : MonoBehaviour {
    [SerializeField] private float rotationSpeed = 1;
    [SerializeField] private MusicBoxRoll roll;
    [SerializeField] private MusicBoxPart[] parts;
    [SerializeField] private bool playOnAwake;
    [SerializeField] private EventReference questFinishedEvent;
    [SerializeField] private GameObject overlay;
    
    private Interactive _interactive;
    private bool _interacting;
    private bool _isFinished;

    private void Awake() {
        _interactive = GetComponent<Interactive>();
    }

    void Start() {
        if (playOnAwake) Play();
    }

    private void Play() {
        if (parts.ToArray().Count(p => p.isActiveAndEnabled) > 0
            && parts.ToArray().Where(p => p.isActiveAndEnabled).Count(p => !p.IsRight()) == 0) {
            overlay.SetActive(true);
            _isFinished = true;
            questFinishedEvent.Raise();
        }
        
        _interacting = true;
        
        foreach (var musicBoxPart in parts) {
            musicBoxPart.Play();
        }
        
        roll.Play();
    }

    private void InteractiveOnOnStartInteract(IInteractiveUser obj) {
        if (_isFinished) return;
        
        Play();
    }

    private void InteractiveOnOnStopInteract(IInteractiveUser obj) {
        if (_isFinished) return;
        
        _interacting = false;
        transform.localEulerAngles = Vector3.zero;
        
        foreach (var musicBoxPart in parts) {
            musicBoxPart.Stop();
        }
        
        roll.Stop();
    }

    void Update() {
        if (_interacting) {
            transform.Rotate(Vector3.right * (Time.deltaTime * rotationSpeed));
        }
    }

    private void OnEnable() {
        _interactive.OnStartInteract += InteractiveOnOnStartInteract;
        _interactive.OnStopInteract += InteractiveOnOnStopInteract;
    }

    private void OnDisable() {
        _interactive.OnStartInteract -= InteractiveOnOnStartInteract;
        _interactive.OnStopInteract -= InteractiveOnOnStopInteract;
    }
}
