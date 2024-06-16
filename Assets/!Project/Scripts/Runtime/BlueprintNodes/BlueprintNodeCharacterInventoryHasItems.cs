using System;
using MisterGames.Actors;
using MisterGames.Blueprints;
using MisterGames.Character.Inventory;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceCharacterInventoryHasItems :
        BlueprintSource<BlueprintNodeCharacterInventoryHasItems>,
        BlueprintSources.IOutput<BlueprintNodeCharacterInventoryHasItems, bool>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Has Inventory Items", Category = "Character", Color = BlueprintColors.Node.Actions)]
    public struct BlueprintNodeCharacterInventoryHasItems :
        IBlueprintNode,
        IBlueprintOutput<bool>
    {
        [SerializeField] private InventoryItemAsset _asset;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Input<IActor>());
            meta.AddPort(id, Port.Input<InventoryItemAsset>("Item Asset"));
            meta.AddPort(id, Port.Output<bool>("Has item"));
        }

        bool IBlueprintOutput<bool>.GetPortValue(IBlueprint blueprint, NodeToken token, int port) {
            var characterAccess = blueprint.Read<IActor>(token, port: 0);
            var inventory = characterAccess.GetComponent<CharacterInventoryPipeline>().Inventory;
            var asset = blueprint.Read(token, port: 1, _asset);
            
            return port switch {
                2 => inventory.ContainsItems(asset),
                _ => default,
            };
        }
    }

}
