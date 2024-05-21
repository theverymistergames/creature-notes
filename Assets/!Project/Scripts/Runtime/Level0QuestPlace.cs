using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;

public class Level0QuestPlace : MonoBehaviour {
    
    [SerializeField] private MultiStateItem[] items;
    [SerializeField] private GameObject dissolveTrigger;

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

        dissolveTrigger.SetActive(true);
        evt.Raise();
    }
}
