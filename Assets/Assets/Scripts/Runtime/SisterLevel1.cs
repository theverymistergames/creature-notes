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
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SisterLevel1 : MonoBehaviour {
    [SerializeField] private GameObject _reactionContainer;
    [SerializeField] private SpriteRenderer _reactionImage;

    [SerializeField] private List<Sprite> _reactions = new List<Sprite>();

    private Interactive _interactive;

    [SerializeField] TweenRunner _bubbleStandard;
    [SerializeField] TweenRunner _bubbleHide;
    
    [SerializeField] private Interactive yesImage, noImage;

    [SerializeField] private EventReference startBattle;
    
    void Start() {
        _reactionContainer.transform.localScale = Vector3.zero;
        
        _interactive = GetComponent<Interactive>();
        
        _interactive.OnStartInteract += OnStartInteract;

        _reactionImage.sprite = _reactions[0];
        _bubbleStandard = GetComponent<TweenRunner>();

        noImage.OnStartInteract += _ => OnAnswerChosen(false);
        yesImage.OnStartInteract += _ => OnAnswerChosen(true);
    }

    private void OnAnswerChosen(bool isYes) {
        _bubbleStandard.TweenPlayer.Stop();
            
        _bubbleHide.TweenPlayer.Progress = 0f;
        _bubbleHide.TweenPlayer.Speed = 1f;
        
        _bubbleHide.TweenPlayer.Play().Forget();

        if (isYes) {
            _interactive.OnStartInteract -= OnStartInteract;
            startBattle.Raise();
        }
    }

    private void OnStartInteract(IInteractiveUser obj) {
        PlayBubbleTween();
    }

    void PlayBubbleTween() {
        _bubbleStandard.TweenPlayer.Progress = 0f;
        _bubbleStandard.TweenPlayer.Speed = 1f;
        
        _bubbleStandard.TweenPlayer.Play().Forget();
    }
}
