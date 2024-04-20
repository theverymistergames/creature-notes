using System;
using MisterGames.Blueprints;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceIntToFloat :
        BlueprintSource<BlueprintNodeIntToFloat>,
        BlueprintSources.IOutput<BlueprintNodeIntToFloat, float>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Int to Float", Category = "Math", Color = BlueprintColors.Node.Data)]
    public struct BlueprintNodeIntToFloat : IBlueprintNode, IBlueprintOutput<float> {

        private int _value;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Input<int>());
            meta.AddPort(id, Port.Output<float>());
        }

        public float GetPortValue(IBlueprint blueprint, NodeToken token, int port) => port switch {
            1 => (float)blueprint.Read(token, 0, _value),
            _ => default,
        };
    }

}
