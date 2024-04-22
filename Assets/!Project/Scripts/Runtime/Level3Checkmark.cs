using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks.Triggers;
using MisterGames.Collisions.Triggers;
using MisterGames.Interact.Detectables;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Events;

public class Level3Checkmark : MonoBehaviour {
    [SerializeField] private TweenRunner[] tweens;
    [SerializeField] private GameObject checkMark;
    [SerializeField] private Trigger startBattleTrigger;
    
    private int _step;
    private Detectable _detectable;

    [NonSerialized] public readonly UnityEvent Flew = new();
    
    void Start() {
        _detectable = checkMark.GetComponent<Detectable>();
        _detectable.OnDetectedBy += DetectableOnOnDetectedBy;
        
        checkMark.SetActive(false);

        startBattleTrigger.OnTriggered += StartBattleTriggerOnOnTriggered;
    }

    private void StartBattleTriggerOnOnTriggered(GameObject obj) {
        Debug.Log("POSHEL");
        startBattleTrigger.gameObject.SetActive(false);
    }

    public void StartSequence() {
        checkMark.SetActive(true);
        MoveToNextPosition();
    }

    private void DetectableOnOnDetectedBy(IDetector obj) {
        MoveToNextPosition();
    }

    private async void MoveToNextPosition() {
        _detectable.enabled = false;

        if (_step == 2) {
            Flew.Invoke();
        }

        var tweenRunner = tweens[_step];
        
        tweenRunner.TweenPlayer.Speed = 1;
        tweenRunner.TweenPlayer.Progress = 0;
        
        await tweens[_step].TweenPlayer.Play();
        
        _detectable.enabled = true;

        _step++;
    }
}
