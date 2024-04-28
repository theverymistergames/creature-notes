using System;
using MisterGames.Blueprints;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceGetParentGameObject :
        BlueprintSource<BlueprintNodeGetParentGameObject>,
        BlueprintSources.IOutput<BlueprintNodeGetParentGameObject, GameObject>,
        BlueprintSources.ICloneable { }

    [Serializable]
    [BlueprintNode(Name = "Get Component GameObject", Category = "GameObject", Color = BlueprintColors.Node.Data)]
    public struct BlueprintNodeGetParentGameObject : IBlueprintNode, IBlueprintOutput<GameObject> {
        
        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Input<Component>());
            meta.AddPort(id, Port.Output<GameObject>());
        }

        public GameObject GetPortValue(IBlueprint blueprint, NodeToken token, int port) {
            if (port != 1) return default;
            
            var component = blueprint.Read<Component>(token, 0);
            return component == null ? default : component.gameObject;
        }
    }

}