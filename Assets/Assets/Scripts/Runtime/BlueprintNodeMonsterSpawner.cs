using System;
using MisterGames.Blueprints;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceMonsterSpawner :
        BlueprintSource<BlueprintNodeMonsterSpawner>,
        BlueprintSources.IEnter<BlueprintNodeMonsterSpawner>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Monster Spawner", Category = "Gameplay", Color = BlueprintColors.Node.Actions)]
    public struct BlueprintNodeMonsterSpawner : IBlueprintNode, IBlueprintEnter {

        private MonsterSpawner spawner;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Enter("Start"));
            meta.AddPort(id, Port.Enter("Stop"));
            meta.AddPort(id, Port.Input<MonsterSpawner>());
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            if (port == 0)
            {
                spawner = blueprint.Read(token, 2, spawner);
                spawner.StartSpawn();
            } else if (port == 1)
            {
                spawner = blueprint.Read(token, 2, spawner);
                spawner.Stop();
            }
            else
            {
                return;
            }

        }
    }

}