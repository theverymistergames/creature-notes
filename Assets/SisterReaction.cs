using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using DigitalRuby.Tween;
using MisterGames.Blueprints;
using MisterGames.Interact.Interactives;
using MisterGames.Tweens;
using MisterGames.Tweens.Core;
using UnityEngine;
using Random = UnityEngine.Random;

public class SisterReaction : MonoBehaviour {
    [SerializeField] private ExerciseBook _exerciseBook;
    [SerializeField] private GameObject _reactionContainer;
    [SerializeField] private SpriteRenderer _reactionImage;

    [SerializeField] private List<Sprite> _reactions = new List<Sprite>();

    private Interactive _interactive;

    private FloatTween _tweenTest;
    private TweenRunner _runner;

    private bool exerciseStarted = false;
    
    void Start()
    {
        TweenFactory.ClearTweensOnLevelLoad = true;
        TweenFactory.Clear();
        
        _reactionContainer.transform.localScale = Vector3.zero;
        
        _exerciseBook.done.AddListener(OnExerciseDone);
        _exerciseBook.started.AddListener(OnExerciseStarted);
        _interactive = GetComponent<Interactive>();
        
        _interactive.OnStartInteract += OnStartInteract;

        _reactionImage.sprite = _reactions[0];
        _runner = GetComponent<TweenRunner>();
    }

    void OnExerciseStarted() {
        exerciseStarted = true;
    }

    private void OnDestroy() {
        _interactive.OnStartInteract -= OnStartInteract;
    }

    private void OnStartInteract(IInteractiveUser obj) {
        PlayBubbleTween();
    }

    void PlayBubbleTween() {
        if (exerciseStarted) {
            _reactionImage.sprite = _reactions[Random.Range(1, 3)];
        }
        
        _runner.Rewind();
        _runner.Play();
    }

    void OnExerciseDone() {
               
    }
}
