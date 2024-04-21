using System;
using MisterGames.Blueprints;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceInt :
        BlueprintSource<BlueprintNodeInt>,
        BlueprintSources.IEnter<BlueprintNodeInt>,
        BlueprintSources.IOutput<BlueprintNodeInt, int>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Int", Category = "Math", Color = BlueprintColors.Node.Data)]
    public struct BlueprintNodeInt : IBlueprintNode, IBlueprintEnter, IBlueprintOutput<int> {

        [SerializeField] private int _value;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Enter("Set"));
            meta.AddPort(id, Port.Input<int>());
            meta.AddPort(id, Port.Output<int>());
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            if (port != 0) return;

            _value = blueprint.Read(token, 1, _value);
        }

        public int GetPortValue(IBlueprint blueprint, NodeToken token, int port) => port switch {
            2 => _value,
            _ => default,
        };
    }

}
