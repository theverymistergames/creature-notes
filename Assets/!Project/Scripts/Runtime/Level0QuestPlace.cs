using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;

public class Level0QuestPlace : MonoBehaviour {
    
    [SerializeField] private MultiStateItem[] items;
    [SerializeField] private GameObject easel;

    public EventReference evt;

    private void OnEnable() {
        for (int i = 0; i < items.Length; i++) {
            items[i].Placed += OnItemPlaced;
        }
    }

    private void OnDisable() {
        for (int i = 0; i < items.Length; i++) {
            items[i].Placed -= OnItemPlaced;
        }
    }

    private void OnItemPlaced() {
        for (int i = 0; i < items.Length; i++) {
            if (!items[i].IsPlacedRight()) return;
        }
        
        for (int i = 0; i < items.Length; i++) {
            items[i].GetComponent<Interactive>().enabled = false;
        }

        easel.GetComponent<Dissolve>().StartDissolve();
        evt.Raise();
    }
}
