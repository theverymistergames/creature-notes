using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using MisterGames.Tweens;
using UnityEngine;

public sealed class SisterLevel0 : MonoBehaviour, IEventListener {
    
    [SerializeField] private GameObject reactionContainer;
    [SerializeField] private SpriteRenderer reactionImage;

    [SerializeField] private List<Sprite> reactions = new();
    [SerializeField] private TweenRunner bookTween;

    [SerializeField] private int placesCountTarget = 0;

    [SerializeField] private EventReference itemsPlacedEvent;
    [SerializeField] private EventReference questGivenEvent;

    private Interactive _interactive;
    private TweenRunner _runner;
    private Book _book;

    private bool _questFinished;

    private void Awake() {
        _interactive = GetComponent<Interactive>();
        _book = bookTween.GetComponent<Book>();
        _book.SetInteractive(false);
        _runner = GetComponent<TweenRunner>();
        
        reactionContainer.transform.localScale = Vector3.zero;
        reactionImage.sprite = reactions[0];

        if (placesCountTarget == 0) _questFinished = true;
    }

    private void OnEnable() {
        itemsPlacedEvent.Subscribe(this);
        _interactive.OnStartInteract += OnStartInteract;
    }

    private void OnDisable() {
        itemsPlacedEvent.Unsubscribe(this);
        _interactive.OnStartInteract -= OnStartInteract;
    }
    
    private void OnStartInteract(IInteractiveUser obj) {
        PlayBubbleTween();

        if (_questFinished) {
            _interactive.enabled = false;
            StartCoroutine(GiveBook());
        }
        
        if (!questGivenEvent.IsRaised()) questGivenEvent.Raise();
    }

    private void PlayBubbleTween() {
        _runner.TweenPlayer.Progress = 0f;
        _runner.TweenPlayer.Speed = 1f;
        _runner.TweenPlayer.Play().Forget();
    }

    private IEnumerator GiveBook() {
        reactionImage.sprite = reactions[1];
        PlayBubbleTween();
        
        yield return new WaitForSeconds(1f);
        bookTween.TweenPlayer.Play().Forget();
        
        _book.SetInteractive(true);
    }

    public void OnEventRaised(EventReference e) {
        if (e.GetCount() >= placesCountTarget) _questFinished = true;
    }
}
