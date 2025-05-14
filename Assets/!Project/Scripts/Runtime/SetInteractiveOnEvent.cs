using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;

public sealed class SetInteractiveOnEvent : MonoBehaviour, IEventListener {

    [SerializeField] private EventReference enableEvent;
    [SerializeField] private EventReference disableEvent;
    [SerializeField] private bool disableOnStart;
    [SerializeField] public bool useInteractive;

    private Collider _collider;
    private Interactive _interactive;

    private void Awake() {
        _collider = GetComponent<BoxCollider>();
        _interactive = GetComponent<Interactive>();
        
        enableEvent.Subscribe(this);
        disableEvent.Subscribe(this);

        if (disableOnStart) {
            _collider.enabled = false;
            if (useInteractive) _interactive.enabled = false;
        }

        if (enableEvent.IsRaised()) OnEventRaised(enableEvent);
        if (disableEvent.IsRaised()) OnEventRaised(disableEvent);
    }

    private void OnDestroy() {
        enableEvent.Unsubscribe(this);
        disableEvent.Unsubscribe(this);
    }

    public void OnEventRaised(EventReference e) {
        if (e.EventId == enableEvent.EventId) {
            _collider.enabled = true;
            if (useInteractive) _interactive.enabled = true;
            return;
        }

        if (e.EventId == disableEvent.EventId) {
            _collider.enabled = false;
            if (useInteractive) _interactive.enabled = false;
        }
    }
}
