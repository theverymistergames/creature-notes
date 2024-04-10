using System.Linq;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;

public class Level0QuestPlace : MonoBehaviour {
    [SerializeField] private MultiStateItem[] items;
    [SerializeField] private GameObject easel;

    public EventReference evt;

    private void Start() {
        foreach (var item in items) {
            item.Placed.AddListener(OnItemPlaced);
        }
    }

    private void OnItemPlaced() {
        if (!items.All(item => item.IsPlacedRight())) return;
        
        foreach (var multiStateItem in items) {
            multiStateItem.GetComponent<Interactive>().enabled = false;
        }
            
        easel.GetComponent<Dissolve>().StartDissolve();
        
        evt.Raise();
    }
}
