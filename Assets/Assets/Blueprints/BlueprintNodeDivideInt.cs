using System;
using MisterGames.Blueprints;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceDivideInt :
        BlueprintSource<BlueprintNodeDivideInt>,
        BlueprintSources.IOutput<BlueprintNodeDivideInt, float>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Divide Int", Category = "Math", Color = BlueprintColors.Node.Data)]
    public struct BlueprintNodeDivideInt : IBlueprintNode, IBlueprintOutput<float> {

        [SerializeField] private int _a;
        [SerializeField] private int _b;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Input<int>("A"));
            meta.AddPort(id, Port.Input<int>("B"));
            meta.AddPort(id, Port.Output<float>());
        }

        public float GetPortValue(IBlueprint blueprint, NodeToken token, int port) {
            if (port == 2) {
                var div = blueprint.Read(token, 1, _b);
                
                if (div == 0) {
                    Debug.LogWarning("Divide by 0");
                    return default;
                }
                
                return blueprint.Read(token, 0, _a) / div;
            }
            
            return default;
        }
    }
}
