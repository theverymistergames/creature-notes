using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.Interact.Interactives;
using UnityEngine;

public class MusicBoxButtonGroup : MonoBehaviour {
    [SerializeField] private GameObject cylinder;
    [SerializeField] private Interactive buttonUp;
    [SerializeField] private Interactive buttonDown;

    private MusicBoxPart _part;
    private AudioSource _source;
    
    void Start() {
        _part = cylinder.GetComponent<MusicBoxPart>();
        _source = GetComponent<AudioSource>();
    }

    private void OnEnable() {
        buttonUp.OnStartInteract += ButtonUpOnOnStartInteract;
        buttonDown.OnStartInteract += ButtonDownOnOnStartInteract;
    }

    private void ButtonDownOnOnStartInteract(IInteractiveUser obj) {
        if (!_part.isActiveAndEnabled) return;
        
        _source.Play();
        _part.Decrease();
    }

    private void ButtonUpOnOnStartInteract(IInteractiveUser obj) {
        if (!_part.isActiveAndEnabled) return;
        
        _source.Play();
        _part.Increase();
    }

    private void OnDisable() {
        buttonUp.OnStartInteract -= ButtonUpOnOnStartInteract;
        buttonDown.OnStartInteract -= ButtonDownOnOnStartInteract;
    }
}
