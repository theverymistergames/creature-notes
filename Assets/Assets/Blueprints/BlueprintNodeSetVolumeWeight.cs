using System;
using MisterGames.Blueprints;
using UnityEngine;
using UnityEngine.Rendering;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceSetVolumeWeight :
        BlueprintSource<BlueprintNodeSetVolumeWeight>,
        BlueprintSources.IEnter<BlueprintNodeSetVolumeWeight>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Set volume weight", Category = "Post processing", Color = BlueprintColors.Node.Actions)]
    public struct BlueprintNodeSetVolumeWeight : IBlueprintNode, IBlueprintEnter {

        [SerializeField] private float _value;
        [SerializeField] private Volume _volume;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Enter("Set"));
            meta.AddPort(id, Port.Input<float>());
            meta.AddPort(id, Port.Input<Volume>());
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            if (port != 0) {
                return;
            }

            _value = blueprint.Read(token, 1, _value);
            _volume = blueprint.Read(token, 2, _volume);
            _volume.weight = _value;
        }
    }

}