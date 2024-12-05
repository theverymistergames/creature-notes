using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;

public class Level2FakeMonstersController : MonoBehaviour, IEventListener {
    [SerializeField] private List<Level2FakeMonster> fakeMonsters;
    [SerializeField] private EventReference partEvent;
    

    public void OnEventRaised(EventReference e) {
        if (e.EventId == partEvent.EventId) {
            var count = e.GetCount();
            var counter = 0;
            
            foreach (var m in fakeMonsters) {
                m.enabled = counter <= count;
                counter++;
            }
        }
    }
    
    void OnEnable() {
        partEvent.Subscribe(this);
    }

    private void OnDisable() {
        partEvent.Unsubscribe(this);
        
        fakeMonsters.ForEach(m => {
            if (m) m.enabled = false;
        });
    }
}
