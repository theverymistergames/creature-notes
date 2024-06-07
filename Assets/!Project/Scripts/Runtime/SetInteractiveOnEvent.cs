using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using MisterGames.Interact.Detectables;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using Unity.VisualScripting;
using UnityEngine;
using Debug = System.Diagnostics.Debug;

public class SetInteractiveOnEvent : MonoBehaviour, IEventListener {

    [SerializeField] private EventReference enableEvent;
    [SerializeField] private EventReference disableEvent;
    [SerializeField] private bool disableOnStart;

    private Collider _collider;

    private void Awake() {
        _collider = GetComponent<BoxCollider>();
        
        enableEvent.Subscribe(this);
        disableEvent.Subscribe(this);

        if (disableOnStart) _collider.enabled = false;
    }

    public void OnEventRaised(EventReference e) {
        if (e.EventId == enableEvent.EventId) {
            _collider.enabled = true;
        } else if (e.EventId == disableEvent.EventId) {
            _collider.enabled = false;
        }
    }
}
