using System;
using MisterGames.Blueprints;
using MisterGames.Character.Core;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceSpawnHeroAtPoint :
        BlueprintSource<BlueprintNodeSpawnHeroAtPoint>,
        BlueprintSources.IEnter<BlueprintNodeSpawnHeroAtPoint>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Spawn hero at point", Category = "Character", Color = BlueprintColors.Node.Actions)]
    public struct BlueprintNodeSpawnHeroAtPoint : IBlueprintNode, IBlueprintEnter {
        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Enter("Spawn"));
            meta.AddPort(id, Port.Input<CharacterAccess>("Access"));
            meta.AddPort(id, Port.Input<Transform>("Point"));
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            if (port != 0) return;

            var access = blueprint.Read<CharacterAccess>(token, 1);
            var point = blueprint.Read<Transform>(token, 2);
            access.BodyAdapter.Position = point.position;
            access.BodyAdapter.Rotation = Quaternion.Euler(point.eulerAngles);
        }

        public void OnValidate(IBlueprintMeta meta, NodeId id) {
            meta.InvalidateNode(id, invalidateLinks: true);
        }
    }

}
