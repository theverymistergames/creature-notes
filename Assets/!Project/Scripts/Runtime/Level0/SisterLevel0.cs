using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Async;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using MisterGames.Tweens;
using UnityEngine;

public sealed class SisterLevel0 : MonoBehaviour {
    
    [Header("Reactions")]
    [SerializeField] private GameObject reactionContainer;
    [SerializeField] private SpriteRenderer reactionImage;
    [SerializeField] private List<Sprite> reactions = new();
    [SerializeField] private Sprite _spriteReactionNeedCleanup;
    [SerializeField] private Sprite _spriteReactionCleanupCompleted;
    
    [Header("Events")]
    [SerializeField] private EventReference questGivenEvent;
    [SerializeField] private EventReference questCompletedEvent;

    [Header("Book")]
    [SerializeField] [Min(0f)] private float _delayBeforeGivingBook = 1f;
    [SerializeField] private TweenRunner bookTween;
    
    private CancellationTokenSource _enableCts;
    private Interactive _interactive;
    private TweenRunner _runner;
    private Book _book;

    private void Awake() {
        _interactive = GetComponent<Interactive>();
        _book = bookTween.GetComponent<Book>();
        _book.SetInteractive(false);
        _runner = GetComponent<TweenRunner>();
        
        reactionContainer.transform.localScale = Vector3.zero;
        reactionImage.sprite = _spriteReactionNeedCleanup;
    }

    private void OnEnable() {
        AsyncExt.RecreateCts(ref _enableCts);
        _interactive.OnStartInteract += OnStartInteract;
    }

    private void OnDisable() {
        AsyncExt.DisposeCts(ref _enableCts);
        _interactive.OnStartInteract -= OnStartInteract;
    }
    
    private void OnStartInteract(IInteractiveUser obj) {
        if (questCompletedEvent.IsRaised()) {
            _interactive.enabled = false;
            GiveBook(_enableCts.Token).Forget();
            return;
        }
        
        PlayBubbleTween();
        if (!questGivenEvent.IsRaised()) questGivenEvent.Raise();
    }

    private void PlayBubbleTween() {
        _runner.TweenPlayer.Progress = 0f;
        _runner.TweenPlayer.Speed = 1f;
        _runner.TweenPlayer.Play().Forget();
    }

    private async UniTask GiveBook(CancellationToken cancellationToken) {
        reactionImage.sprite = _spriteReactionCleanupCompleted;
        PlayBubbleTween();

        await UniTask.Delay(TimeSpan.FromSeconds(_delayBeforeGivingBook), cancellationToken: cancellationToken)
            .SuppressCancellationThrow();
        
        if (cancellationToken.IsCancellationRequested) return;
        
        await bookTween.TweenPlayer.Play(cancellationToken: cancellationToken);
        
        if (cancellationToken.IsCancellationRequested) return;
        
        _book.SetInteractive(true);
    }
}
