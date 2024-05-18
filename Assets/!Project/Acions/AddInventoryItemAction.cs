using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Inventory;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace MisterGames.ActionLib.GameObjects {
    [Serializable]
    public sealed class AddInventoryItemAction : IActorAction {
        public InventoryItemAsset[] items;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            foreach (var inventoryItemAsset in items) {
                context.GetComponent<CharacterInventoryPipeline>().Inventory.AddItems(inventoryItemAsset, 1);
            }
            
            return default;
        }
    }
    
}