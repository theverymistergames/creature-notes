using System.Collections;
using System.Collections.Generic;
using MisterGames.Interact.Interactives;
using UnityEngine;

public class InventoryItem : MonoBehaviour {
    [SerializeField] private InventoryItemType id = InventoryItemType.BALL;
    
    private Interactive _interactive;
    
    void Start()
    {
        _interactive = GetComponent<Interactive>();
        _interactive.OnStartInteract += OnStartInteract;
    }

    private void OnStartInteract(IInteractiveUser user) {
        Inventory.instance.AddItem(id);
        gameObject.SetActive(false);
    }
}
