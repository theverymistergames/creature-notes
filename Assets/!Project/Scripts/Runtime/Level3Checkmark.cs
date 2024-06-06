using System;
using Cysharp.Threading.Tasks;
using MisterGames.Collisions.Triggers;
using MisterGames.Interact.Detectables;
using MisterGames.Scenario.Events;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Events;

public class Level3Checkmark : MonoBehaviour {
    [SerializeField] private TweenRunner[] tweens;
    [SerializeField] private GameObject checkMark;
    [SerializeField] private DirectionalTrigger startBattleTrigger;

    [SerializeField] private GameObject sister, sisterScreamer;

    [SerializeField] private EventReference startBattleEvent;
    
    private int _step;
    private Detectable _detectable;

    [NonSerialized] public readonly UnityEvent Flew = new();
    
    void Start() {
        _detectable = checkMark.GetComponent<Detectable>();
        _detectable.OnDetectedBy += DetectableOnOnDetectedBy;
        
        checkMark.SetActive(false);

        startBattleTrigger.OnTriggeredForward += StartBattleTriggerOnOnTriggeredForward;
    }

    private async void StartBattleTriggerOnOnTriggeredForward(GameObject obj) {
        sister.SetActive(false);
        sisterScreamer.SetActive(true);

        await UniTask.Delay(1000);
        
        sisterScreamer.SetActive(false);
        
        startBattleEvent.Raise();
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

        switch (_step) {
            case 2:
                Flew.Invoke();
                break;
            case 6:
                startBattleTrigger.gameObject.SetActive(true);
                break;
        }
        
        await tweens[_step].TweenPlayer.Play(progress: 0);
        
        _detectable.enabled = true;

        _step++;
    }
}
