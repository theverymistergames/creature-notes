using System;
using MisterGames.Blueprints;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceAddInt :
        BlueprintSource<BlueprintNodeAddInt>,
        BlueprintSources.IOutput<BlueprintNodeAddInt, int>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Add Int", Category = "Math", Color = BlueprintColors.Node.Data)]
    public struct BlueprintNodeAddInt : IBlueprintNode, IBlueprintOutput<int> {

        [SerializeField] private int _a;
        [SerializeField] private int _b;
        
        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Input<int>("A"));
            meta.AddPort(id, Port.Input<int>("B"));
            meta.AddPort(id, Port.Output<int>());
        }

        public int GetPortValue(IBlueprint blueprint, NodeToken token, int port) => port switch {
            2 => blueprint.Read(token, 0, _a) + blueprint.Read(token, 1, _b),
            _ => default,
        };
    }

}
