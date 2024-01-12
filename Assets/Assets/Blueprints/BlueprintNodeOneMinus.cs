using System;
using MisterGames.Blueprints;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceOneMinus :
        BlueprintSource<BlueprintNodeOneMinus>,
        BlueprintSources.IOutput<BlueprintNodeOneMinus, float>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "One minus", Category = "Math", Color = BlueprintColors.Node.Data)]
    public struct BlueprintNodeOneMinus : IBlueprintNode, IBlueprintOutput<float> {

        private float _value;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Input<float>());
            meta.AddPort(id, Port.Output<float>());
        }

        public float GetPortValue(IBlueprint blueprint, NodeToken token, int port) => port switch {
            1 => 1 - blueprint.Read(token, 0, _value),
            _ => default,
        };
    }

}
