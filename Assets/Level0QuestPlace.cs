using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MisterGames.Interact.Interactives;
using UnityEngine;
using UnityEngine.Serialization;

public class Level0QuestPlace : MonoBehaviour {
    [SerializeField] private MultiStateItem[] items;
    
    void Start() {
        foreach (var item in items) {
            item.placed.AddListener(OnItemPlaced);
        }
    }

    private void OnItemPlaced() {
        if (items.All(item => item.IsPlacedRight())) {
            foreach (var multiStateItem in items) {
                multiStateItem.GetComponent<Interactive>().enabled = false;
            }
        }
    }
}
