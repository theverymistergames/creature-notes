using System;
using MisterGames.Blueprints;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceSetLightIntensity :
        BlueprintSource<BlueprintNodeSetLightIntensity>,
        BlueprintSources.IEnter<BlueprintNodeSetLightIntensity>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Set Light intensity", Category = "GameObject", Color = BlueprintColors.Node.Actions)]
    public struct BlueprintNodeSetLightIntensity : IBlueprintNode, IBlueprintEnter {
        [SerializeField] private float intensity;
        private Light light;

        private Color _color;

        public void OnStart(IBlueprint blueprint, NodeToken token) {
            light = blueprint.Read(token, 2, light);
        }
        
        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Enter("Set"));
            meta.AddPort(id, Port.Input<float>());
            meta.AddPort(id, Port.Input<Light>());
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            if (port != 0) {
                return;
            }

            intensity = blueprint.Read(token, 1, intensity);
            light = blueprint.Read(token, 2, light);
            light.intensity = intensity;
        }
    }

}