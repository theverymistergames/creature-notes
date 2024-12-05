using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Serialization;

public class SisterLevel0 : MonoBehaviour, IEventListener {
    [SerializeField] GameObject reactionContainer;
    [SerializeField] SpriteRenderer reactionImage;

    [SerializeField] List<Sprite> reactions = new();
    [SerializeField] TweenRunner bookTween;

    [SerializeField] private int placesCountTarget = 0;

    [SerializeField] EventReference itemsPlacedEvent;
    [SerializeField] EventReference questGivenEvent;

    private int _placesCount = 0;
    Interactive _interactive;
    TweenRunner _runner;
    Book _book;

    bool _questFinished;

    private void Awake() {
        _interactive = GetComponent<Interactive>();
        _book = bookTween.GetComponent<Book>();
        _book.SetInteractive(false);
        _runner = GetComponent<TweenRunner>();
        
        itemsPlacedEvent.Subscribe(this);
        
        reactionContainer.transform.localScale = Vector3.zero;
        reactionImage.sprite = reactions[0];

        if (placesCountTarget == 0) _questFinished = true;
    }

    private void OnStartInteract(IInteractiveUser obj) {
        PlayBubbleTween();

        if (_questFinished) {
            _questFinished = false;
            
            StartCoroutine(GiveBook());
            
            _interactive.enabled = false;
        }
        
        if (questGivenEvent.GetCount() == 0) questGivenEvent.Raise();
    }

    void PlayBubbleTween() {
        _runner.TweenPlayer.Progress = 0f;
        _runner.TweenPlayer.Speed = 1f;
        _runner.TweenPlayer.Play().Forget();
    }

    private IEnumerator GiveBook() {
        reactionImage.sprite = reactions[1];
        PlayBubbleTween();
        
        yield return new WaitForSeconds(1f);
        yield return bookTween.TweenPlayer.Play();
        
        _book.SetInteractive(true);
    }

    public void OnEventRaised(EventReference e) {
        if (e.EventId != itemsPlacedEvent.EventId) return;
        
        if (++_placesCount == placesCountTarget) _questFinished = true;
    }

    private void OnEnable() {
        _interactive.OnStartInteract += OnStartInteract;
    }

    private void OnDisable() {
        _interactive.OnStartInteract -= OnStartInteract;
    }
}
