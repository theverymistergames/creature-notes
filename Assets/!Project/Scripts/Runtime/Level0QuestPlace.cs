using System.Linq;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;

public class Level0QuestPlace : MonoBehaviour {
    [SerializeReference] private Placeable[] placeables;
    [SerializeField] private GameObject dissolveTrigger;

    public EventReference evt;

    private void OnEnable() {
        for (int i = 0; i < placeables.Length; i++) {
            placeables[i].Placed += OnItemPlaced;
        }
    }

    private void OnDisable() {
        for (int i = 0; i < placeables.Length; i++) {
            placeables[i].Placed -= OnItemPlaced;
        }
    }

    private void OnItemPlaced() {
        for (int i = 0; i < placeables.Length; i++) {
            if (!placeables[i].IsPlacedRight()) return;
        }
        
        for (int i = 0; i < placeables.Length; i++) {
            placeables[i].GetComponent<Interactive>().enabled = false;
        }

        dissolveTrigger.SetActive(true);
        evt.Raise();
    }
}
