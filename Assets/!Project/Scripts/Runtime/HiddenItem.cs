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
    
    private Interactive _interactive;

    private void Start() {
        hiddenItem.SetActive(false);
        _interactive = GetComponent<Interactive>();
        
        @event.Subscribe(this);
    }

    public void OnEventRaised(EventReference e) {
        _interactive.OnStartInteract += InteractiveOnOnStartInteract;
    }

    private void InteractiveOnOnStartInteract(IInteractiveUser obj) {
        GetComponent<BoxCollider>().enabled = false;
        hiddenItem.SetActive(true);
    }
}
