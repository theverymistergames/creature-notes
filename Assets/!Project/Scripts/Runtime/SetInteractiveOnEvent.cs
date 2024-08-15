using MisterGames.Scenario.Events;
using UnityEngine;

public sealed class SetInteractiveOnEvent : MonoBehaviour, IEventListener {

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

    private void OnDestroy() {
        enableEvent.Unsubscribe(this);
        disableEvent.Unsubscribe(this);
    }

    public void OnEventRaised(EventReference e) {
        if (e.EventId == enableEvent.EventId) {
            _collider.enabled = true;
            return;
        }

        if (e.EventId == disableEvent.EventId) {
            _collider.enabled = false;
        }
    }
}
