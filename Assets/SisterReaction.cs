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
using Random = System.Random;

public class SisterReaction : MonoBehaviour {
    [SerializeField] private ExerciseBook _exerciseBook;
    [SerializeField] private GameObject _reactionContainer;
    [SerializeField] private SpriteRenderer _reactionImage;

    [SerializeField] private List<Sprite> _reactions = new List<Sprite>();

    private Interactive _interactive;

    private FloatTween _tweenTest;
    private TweenRunner _runner;
    
    void Start()
    {
        TweenFactory.ClearTweensOnLevelLoad = true;
        TweenFactory.Clear();
        
        _reactionContainer.transform.localScale = Vector3.zero;
        
        _exerciseBook.done.AddListener(OnExerciseDone);
        _interactive = GetComponent<Interactive>();
        
        _interactive.OnStartInteract += OnStartInteract;

        _reactionImage.sprite = _reactions[0];
        _runner = GetComponent<TweenRunner>();
    }

    private void OnDestroy() {
        _interactive.OnStartInteract -= OnStartInteract;
    }

    private void OnStartInteract(IInteractiveUser obj) {
        if (_tweenTest is { State: TweenState.Running }) _tweenTest.Stop(TweenStopBehavior.DoNotModify);
        
        _reactionContainer.transform.localScale = Vector3.zero;

        var pt = new ProgressTween();
        pt.Initialize(this);
        pt.Play(CancellationToken.None);

        var runner = new BlueprintRunner();

        _tweenTest = TweenFactory.Tween(RandomNumberGenerator.Create(), 0, 1, 0.2f, TweenScaleFunctions.CubicEaseOut,
            (t) => {
                Debug.Log(t.CurrentValue);
                _reactionContainer.transform.localScale = new Vector3(t.CurrentValue, t.CurrentValue, t.CurrentValue);
            }, tween => {
                _tweenTest = TweenFactory.Tween(null, 0, 1, 1.5f, TweenScaleFunctions.CubicEaseOut,
                    (t) => {}, ttt => {
                        _tweenTest = TweenFactory.Tween(null, 1, 0, 0.2f, TweenScaleFunctions.CubicEaseOut,
                            (t) => {
                                _reactionContainer.transform.localScale = new Vector3(t.CurrentValue, t.CurrentValue, t.CurrentValue);
                            });
                    });
            });
    }

    void OnExerciseDone() {
               
    }
}
