using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.Interact.Detectables;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Events;

public class Level3Checkmark : MonoBehaviour {
    [SerializeField] private TweenRunner[] tweens;
    [SerializeField] private GameObject checkMark;
    [SerializeField] private GameObject yesIcon;
    
    private int step = 0;
    private Detectable _detectable;

    [NonSerialized] public UnityEvent Flew = new();
    
    void Start() {
        _detectable = checkMark.GetComponent<Detectable>();
        _detectable.OnDetectedBy += DetectableOnOnDetectedBy;
        
        checkMark.SetActive(false);
    }

    public void StartSequence() {
        checkMark.SetActive(true);
        
        MoveToNextPosition();
    }

    private void DetectableOnOnDetectedBy(IDetector obj) {
        MoveToNextPosition();
    }

    public async void MoveToNextPosition() {
        _detectable.enabled = false;

        if (step == 2) {
            Flew.Invoke();
        }

        var tweenRunner = tweens[step];
        
        tweenRunner.TweenPlayer.Speed = 1;
        tweenRunner.TweenPlayer.Progress = 0;
        
        await tweens[step].TweenPlayer.Play();
        
        _detectable.enabled = true;

        step++;
    }
}
