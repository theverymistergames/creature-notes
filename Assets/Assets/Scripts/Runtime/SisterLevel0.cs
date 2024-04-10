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
    [SerializeField] Interactive[] interactives;

    [SerializeField] private int placesCountTarget = 0;

    private int _placesCount = 0;
    Interactive _interactive;
    TweenRunner _runner;

    [SerializeField] EventReference itemsPlacedEvent;

    bool _interacted;
    bool _readyToGiveBook;

    private void Start() {
        reactionContainer.transform.localScale = Vector3.zero;
        
        _interactive = GetComponent<Interactive>();
        _interactive.OnStartInteract += OnStartInteract;

        reactionImage.sprite = reactions[0];
        
        foreach (var interactive in interactives) {
            interactive.enabled = false;
        }
        
        _runner = GetComponent<TweenRunner>();
        
        itemsPlacedEvent.Subscribe(this);
    }

    private void OnStartInteract(IInteractiveUser obj) {
        PlayBubbleTween();

        if (_readyToGiveBook) {
            _readyToGiveBook = false;
            
            StartCoroutine(GiveBook());
            
            _interactive.enabled = false;
        }

        if (_interacted) return;
        
        _interacted = true;
            
        foreach (var interactive in interactives) {
            interactive.enabled = true;
        }
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
        
        bookTween.TweenPlayer.Play().Forget();
    }

    public void OnEventRaised(EventReference e) {
        _placesCount++;
        
        if (_placesCount == placesCountTarget) {
            _readyToGiveBook = true;
        }
    }
}
