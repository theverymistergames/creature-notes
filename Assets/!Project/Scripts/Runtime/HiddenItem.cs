using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using MisterGames.Character.Core;
using MisterGames.Character.Inventory;
using MisterGames.Interact.Detectables;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class HiddenItem : MonoBehaviour, IEventListener {
    [SerializeField] private EventReference @event;
    [SerializeField] private GameObject hiddenItem;
    [SerializeField] private GameObject contour;
    [SerializeField] private bool activateGameObjectOnPlaced = false;
    [SerializeField] private GameObject goOnPlaced;
    
    private Detectable _detectable;
    private Interactive _interactive;
    private Collider _collider;

    private void Start() {
        if (activateGameObjectOnPlaced) goOnPlaced.SetActive(false);
        
        hiddenItem.SetActive(false);
        contour.SetActive(false);

        _collider = GetComponent<BoxCollider>();
        _collider.enabled = false;

        GetComponent<Interactive>().OnStartInteract += InteractiveOnOnStartInteract;
        
        @event.Subscribe(this);
    }

    public void OnEventRaised(EventReference e) {
        if (e.EventId != @event.EventId) return;
        
        contour.SetActive(true);
        _collider.enabled = true;
    }

    private void InteractiveOnOnStartInteract(IInteractiveUser obj) {
        contour.SetActive(false);
        _collider.enabled = false;
        hiddenItem.SetActive(true);
        
        if (activateGameObjectOnPlaced) goOnPlaced.SetActive(true);
    }
}
