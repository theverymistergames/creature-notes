using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Common.GameObjects;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;

public sealed class HiddenItem : MonoBehaviour, IEventListener {
    
    [SerializeField] private EventReference @event;
    [SerializeField] private GameObject hiddenItem;
    [SerializeField] private int activateHiddenItemDelay = 50;
    [SerializeField] private GameObject contour;
    [SerializeField] private bool activateGameObjectOnPlaced = false;
    [SerializeField] private GameObject goOnPlaced;
    
    private Interactive _interactive;
    private Collider _collider;

    private void Awake() {
        _collider = GetComponent<BoxCollider>();
        _interactive = GetComponent<Interactive>();
    }

    private void OnEnable() {
        _interactive.OnStartInteract += OnStartInteract;
        @event.Subscribe(this);
    }

    private void OnDisable() {
        _interactive.OnStartInteract -= OnStartInteract;
        @event.Unsubscribe(this);
    }

    private void Start() {
        if (activateGameObjectOnPlaced) goOnPlaced.SetActive(false);
        
        hiddenItem.SetActive(false);
        contour.SetActive(false);
        
        _collider.enabled = false;
    }

    public void OnEventRaised(EventReference e) {
        contour.SetActive(true);
        _collider.enabled = true;
    }

    private void OnStartInteract(IInteractiveUser obj) {
        contour.SetActive(false);
        _collider.enabled = false;
        
        if (activateGameObjectOnPlaced) goOnPlaced.SetActive(true);

        ActivateHiddenItem(activateHiddenItemDelay * 0.001f, destroyCancellationToken).Forget();
    }

    private async UniTask ActivateHiddenItem(float delay, CancellationToken cancellationToken) {
        await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken)
            .SuppressCancellationThrow();
        
        if (cancellationToken.IsCancellationRequested) return;
        
        hiddenItem.SetActive(true);
    }
}
