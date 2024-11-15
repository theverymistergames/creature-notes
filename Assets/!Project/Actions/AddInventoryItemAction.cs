using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Inventory;

namespace MisterGames.ActionLib.GameObjects {
    
    [Serializable]
    public sealed class AddInventoryItemAction : IActorAction {
        
        public InventoryItemAsset[] items;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            var inventory = context.GetComponent<CharacterInventoryPipeline>();
            
            for (int i = 0; i < items.Length; i++) {
                inventory.Inventory.AddItems(items[i], 1);
            }

            return default;
        }
    }
    
}