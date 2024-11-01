using System;
using MisterGames.Blueprints;
using UnityEngine;
using UnityEngine.Serialization;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceIsEnabled :
        BlueprintSource<BlueprintNodeIsEnabled>,
        BlueprintSources.IOutput<BlueprintNodeIsEnabled, bool>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "IsEnabled", Category = "GameObject", Color = BlueprintColors.Node.Data)]
    public struct BlueprintNodeIsEnabled : IBlueprintNode, IBlueprintOutput<bool> {

        [SerializeField] private GameObject go;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Input<GameObject>());
            meta.AddPort(id, Port.Output<bool>());
        }

        public bool GetPortValue(IBlueprint blueprint, NodeToken token, int port) {
            go = blueprint.Read(token, 0, go);
            return port == 1 ? go.activeSelf : default;
        }
    }

}
