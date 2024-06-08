using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.Interact.Interactives;
using UnityEngine;

public class QuestSign : MonoBehaviour {
    [SerializeField] private Interactive interactive;
    
    private void InteractiveOnOnStartInteract(IInteractiveUser obj) {
        gameObject.SetActive(false);
    }
    
    private void OnEnable() {
        interactive.OnStartInteract += InteractiveOnOnStartInteract;
    }

    private void OnDisable() {
        interactive.OnStartInteract -= InteractiveOnOnStartInteract;
    }
}
