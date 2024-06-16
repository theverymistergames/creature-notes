using System;
using MisterGames.Blueprints;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceNot :
        BlueprintSource<BlueprintNodeNot>,
        BlueprintSources.IOutput<BlueprintNodeNot, bool>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Not", Category = "Math", Color = BlueprintColors.Node.Data)]
    public struct BlueprintNodeNot : IBlueprintNode, IBlueprintOutput<bool> {

        private bool _value;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Input<bool>());
            meta.AddPort(id, Port.Output<bool>());
        }

        public bool GetPortValue(IBlueprint blueprint, NodeToken token, int port) => port switch {
            1 => !blueprint.Read(token, 0, _value),
            _ => default,
        };
    }

}
