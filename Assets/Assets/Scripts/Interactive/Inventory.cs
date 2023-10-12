using System.Collections;
using System.Collections.Generic;
using MisterGames.Interact.Interactives;
using UnityEngine;


public enum InventoryItemType {
    BALL,
    VASE,
    BOX,
}
public class Inventory : MonoBehaviour {
    public static Inventory instance;
    
    private List<InventoryItemType> _items = new List<InventoryItemType>();
    
    void Start() {
        if (!instance) {
            instance = this;
        }
    }

    public void AddItem(InventoryItemType type) {
        _items.Add(type);
    }

    public bool HasItem(InventoryItemType type) {
        return _items.Contains(type);
    }

    public void RemoveItem(InventoryItemType type) {
        _items.Remove(type);
    }
}