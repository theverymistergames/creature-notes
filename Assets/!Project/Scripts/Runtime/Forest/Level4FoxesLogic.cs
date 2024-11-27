using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.UI;

public class Level4FoxesLogic : MonoBehaviour, IEventListener
{
    [SerializeField] private EventReference foxFoundEvent;
    [SerializeField] private EventReference caughtEvent;
    [SerializeField] private List<Level4FoxLogic> foxLogics = new();
    [SerializeField] private Image fadeTransitionImage;
    [SerializeField] private float transitionTime = 2f;

    public void OnEventRaised(EventReference e) {
        if (e.EventId == foxFoundEvent.EventId) {
            
        } else if (e.EventId == caughtEvent.EventId) {
            OnCaught();
        }
    }

    private async void OnCaught() {
        LMotion.Create(0f, 1f, transitionTime).Bind(t => {
            var c = fadeTransitionImage.color;
            c.a = t;
            fadeTransitionImage.color = c;
        });
        
        await UniTask.Delay((int)(transitionTime * 1000));
        
        foxFoundEvent.SetCount(foxFoundEvent.GetRaiseCount() - 1);
        var c = foxFoundEvent.GetRaiseCount();
        foxLogics[c].Reset();
        
        await UniTask.Delay((int)(transitionTime * 1000));
        
        LMotion.Create(1f, 0f, transitionTime).Bind(t => {
            var c = fadeTransitionImage.color;
            c.a = t;
            fadeTransitionImage.color = c;
        });
    }
    
    void OnEnable() {
        foxFoundEvent.Subscribe(this);
        caughtEvent.Subscribe(this);
    }

    private void OnDisable() {
        foxFoundEvent.Unsubscribe(this);
        caughtEvent.Unsubscribe(this);
    }
}
