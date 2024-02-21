using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DigitalRuby.Tween;
using MisterGames.Blueprints;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using MisterGames.Tweens;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class Sister : MonoBehaviour {
    [SerializeField] private ExerciseBook _exerciseBook;
    [SerializeField] private GameObject _reactionContainer;
    [SerializeField] private SpriteRenderer _reactionImage;

    [SerializeField] private List<Sprite> _reactions = new List<Sprite>();
    [SerializeField] private TweenRunner _bookTween;

    private Interactive _interactive;

    private FloatTween _tweenTest;
    private TweenRunner _runner;

    private bool exerciseStarted = false;

    [SerializeField] private EventReference evt;
    
    void Start() {
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

    private void OnStartInteract(IInteractiveUser obj) {
        PlayBubbleTween();
    }

    void PlayBubbleTween() {
        if (exerciseStarted) {
            _reactionImage.sprite = _reactions[Random.Range(1, 3)];
        }

        _runner.TweenPlayer.Progress = 0f;
        _runner.TweenPlayer.Speed = 1f;
        _runner.TweenPlayer.Play().Forget();
    }

    void OnExerciseDone()
    {
        StartCoroutine(GiveBook());
    }

    IEnumerator GiveBook()
    {
        PlayBubbleTween();
        
        yield return new WaitForSeconds(0.5f);
        
        _bookTween.TweenPlayer.Play().Forget();
        
        yield return new WaitForSeconds(5f);
        
        evt.Raise();
    }
}
