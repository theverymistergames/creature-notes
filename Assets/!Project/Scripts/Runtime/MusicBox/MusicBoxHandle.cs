using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.BlueprintLib;
using MisterGames.Interact.Interactives;
using UnityEngine;

public class MusicBoxHandle : MonoBehaviour {
    [SerializeField] private float rotationSpeed = 1;
    [SerializeField] private MusicBoxRoll roll;
    [SerializeField] private MusicBoxPart[] parts;
    
    private Interactive _interactive;
    private bool _interacting;
    
    void Start() {
    }

    private void InteractiveOnOnStartInteract(IInteractiveUser obj) {
        _interacting = true;
        
        foreach (var musicBoxPart in parts) {
            musicBoxPart.Play();
        }
        
        roll.Play();
    }

    private void InteractiveOnOnStopInteract(IInteractiveUser obj) {
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
        _interactive = GetComponent<Interactive>();
        
        _interactive.OnStartInteract += InteractiveOnOnStartInteract;
        _interactive.OnStopInteract += InteractiveOnOnStopInteract;
    }

    private void OnDisable() {
        _interactive.OnStartInteract -= InteractiveOnOnStartInteract;
        _interactive.OnStopInteract -= InteractiveOnOnStopInteract;
    }
}
