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

    // [SerializeField] private EventReference startBattle;

    [SerializeField] private Level3Checkmark level3Checkmark;

    private int _tryNumber;
    
    void Start() {
        reactionContainer.transform.localScale = Vector3.zero;
        
        _sisterInteractive = GetComponent<Interactive>();
        _sisterInteractive.OnStartInteract += OnSisterStartInteract;

        noInteractive.OnStartInteract += _ => OnAnswerChosen(false);
        yesInteractive.OnStartInteract += _ => OnAnswerChosen(true);
        
        level3Checkmark.Flew.AddListener(OnCheckmarkFlew);
    }

    private void OnCheckmarkFlew() {
        PlayTweenRunner(bubbleHide);
    }

    private async void OnAnswerChosen(bool isYes) {
        if (isYes) {
            _sisterInteractive.OnStartInteract -= OnSisterStartInteract;
            
            noInteractive.enabled = false;
            yesInteractive.gameObject.SetActive(false);
            
            level3Checkmark.StartSequence();
        } else {
            await PlayTweenRunner(bubbleHide);
            
            _sisterInteractive.enabled = true;
        }
    }

    private async void OnSisterStartInteract(IInteractiveUser obj) {
        _sisterInteractive.enabled = false;
        
        if (_tryNumber >= 2) {
            bubble0.SetActive(false);
            bubble1.SetActive(true);

            await PlayTweenRunner(bubbleFinal);
        } else {
            await PlayTweenRunner(bubbleStart);
        
            _sisterInteractive.enabled = true;
        
            _tryNumber++;
        }
    }

    private UniTask PlayTweenRunner(TweenRunner runner) {
        runner.TweenPlayer.Speed = 1f;
        runner.TweenPlayer.Progress = 0f;
            
        return runner.TweenPlayer.Play();
    }
}
