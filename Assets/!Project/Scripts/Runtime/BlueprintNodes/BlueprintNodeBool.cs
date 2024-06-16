using System;
using MisterGames.Blueprints;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceBool :
        BlueprintSource<BlueprintNodeBool>,
        BlueprintSources.IEnter<BlueprintNodeBool>,
        BlueprintSources.IOutput<BlueprintNodeBool, bool>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Bool", Category = "Math", Color = BlueprintColors.Node.Data)]
    public struct BlueprintNodeBool : IBlueprintNode, IBlueprintEnter, IBlueprintOutput<bool> {

        [SerializeField] private bool _value;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Enter("Set"));
            meta.AddPort(id, Port.Input<bool>());
            meta.AddPort(id, Port.Output<bool>());
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            if (port != 0) return;

            _value = blueprint.Read(token, 1, _value);
        }

        public bool GetPortValue(IBlueprint blueprint, NodeToken token, int port) => port switch {
            2 => _value,
            _ => default,
        };
    }

}
