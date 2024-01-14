using System;
using MisterGames.Blueprints;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceDivideFloat :
        BlueprintSource<BlueprintNodeDivideFloat>,
        BlueprintSources.IOutput<BlueprintNodeDivideFloat, float>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Divide Float", Category = "Math", Color = BlueprintColors.Node.Data)]
    public struct BlueprintNodeDivideFloat : IBlueprintNode, IBlueprintOutput<float> {

        [SerializeField] private float _a;
        [SerializeField] private float _b;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Input<float>("A"));
            meta.AddPort(id, Port.Input<float>("B"));
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
