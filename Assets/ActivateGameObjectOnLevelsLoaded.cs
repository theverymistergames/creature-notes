using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.Common.Lists;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;

public class ActivateGameObjectOnLevelsLoaded : MonoBehaviour, IEventListener {
    [SerializeField] private EventReference levelLoadedEvent;
    [SerializeField] private List<int> levels;
    [SerializeField] private GameObject go;

    public void OnEventRaised(EventReference e) {
        if (e.EventId == levelLoadedEvent.EventId) {
            SetActiveIfNeeded();
        }
    }

    private void SetActiveIfNeeded() {
        var count = levelLoadedEvent.GetRaiseCount();
        
        if (levels.Contains(count)) {
            go.SetActive(true);
        } else {
            go.SetActive(false);
        }
    }

    private void OnEnable() {
        SetActiveIfNeeded();
        levelLoadedEvent.Subscribe(this);
    }

    private void OnDisable() {
        levelLoadedEvent.Unsubscribe(this);
    }
}
