using System.Collections.Generic;
using MisterGames.Actors.Actions;
using MisterGames.Character.Core;
using MisterGames.Character.Inventory;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;

public class Level2Painting : MonoBehaviour {
    [SerializeField] private List<InventoryItemAsset> inventoryItemAssets = new();
    [SerializeField] private List<GameObject> parts = new();
    [SerializeField] private EventReference partsCollectedEvent;
    [SerializeField] private int maxParts = 5;
    [SerializeField] private ActionReference _action;
    
    private Interactive _interactive;
    private IInventory _inventory;
    private int partsCounter = 0;
    
    void Start() {
        _interactive = GetComponent<Interactive>();
        _interactive.OnStartInteract += InteractiveOnOnStartInteract;

        _inventory = CharacterAccessRegistry.Instance.GetCharacterAccess().GetComponent<CharacterInventoryPipeline>().Inventory;
    }

    private void InteractiveOnOnStartInteract(IInteractiveUser obj) {
        var counter = 0;
        
        foreach (var asset in inventoryItemAssets) {
            
            if (_inventory.ContainsItems(asset)) {
                _inventory.RemoveItems(asset, 1);
                
                parts[counter].SetActive(true);

                partsCounter++;
            }
            
            counter++;
        }

        if (partsCounter == maxParts) {
            partsCollectedEvent.Raise();
        }
    }
}
