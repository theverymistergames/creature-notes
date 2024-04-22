using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Serialization;

public class SisterLevel3 : MonoBehaviour {
    [FormerlySerializedAs("_reactionContainer")] [SerializeField] private GameObject reactionContainer;

    private Interactive _sisterInteractive;

    [SerializeField] TweenRunner bubbleStart;
    [SerializeField] TweenRunner bubbleFinal;
    [SerializeField] TweenRunner bubbleHide;

    [SerializeField] private GameObject bubble0, bubble1;
    
    [SerializeField] private Interactive yesInteractive;
    [SerializeField] private Interactive noInteractive;

    [SerializeField] private Level3Checkmark level3Checkmark;

    private int _tryNumber;
    
    void Start() {
        reactionContainer.transform.localScale = Vector3.zero;
        
        _sisterInteractive = GetComponent<Interactive>();
        _sisterInteractive.OnStartInteract += OnSisterStartInteract;

        noInteractive.OnStartInteract += _ => OnAnswerChosen(false);
        yesInteractive.OnStartInteract += _ => OnAnswerChosen(true);
        
        bubble0.SetActive(true);
        bubble1.SetActive(false);
        
        level3Checkmark.Flew.AddListener(OnCheckmarkFlew);
    }

    private void OnCheckmarkFlew() {
        PlayTweenRunner(bubbleHide);
    }

    private async void OnAnswerChosen(bool isYes) {
        if (isYes) {
            _sisterInteractive.OnStartInteract -= OnSisterStartInteract;
            
            yesInteractive.gameObject.SetActive(false);
            
            level3Checkmark.StartSequence();
        } else {
            await PlayTweenRunner(bubbleHide);
            
            _sisterInteractive.OnStartInteract += OnSisterStartInteract;
        }
    }

    private async void OnSisterStartInteract(IInteractiveUser obj) {
        _sisterInteractive.OnStartInteract -= OnSisterStartInteract;
        
        if (_tryNumber >= 2) {
            bubble0.SetActive(false);
            bubble1.SetActive(true);

            await PlayTweenRunner(bubbleFinal);
        } else {
            await PlayTweenRunner(bubbleStart);
        
            _sisterInteractive.OnStartInteract += OnSisterStartInteract;
        
            _tryNumber++;
        }
    }

    private UniTask PlayTweenRunner(TweenRunner runner) {
        return runner.TweenPlayer.Play(progress: 0f);
    }
}
