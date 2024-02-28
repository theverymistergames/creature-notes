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

        private MonsterController _controller;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Enter("Start"));
            meta.AddPort(id, Port.Enter("Stop"));
            meta.AddPort(id, Port.Input<MonsterController>());
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            if (port == 0)
            {
                _controller = blueprint.Read(token, 2, _controller);
                _controller.StartSpawn();
            } else if (port == 1)
            {
                _controller = blueprint.Read(token, 2, _controller);
                _controller.Stop();
            }
            else
            {
                return;
            }

        }
    }

}