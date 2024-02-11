using System;
using MisterGames.Blueprints;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MisterGames.BlueprintLib {

    [Serializable]
    public class BlueprintSourceSetImageOpacity :
        BlueprintSource<BlueprintNodeSetImageOpacity>,
        BlueprintSources.IEnter<BlueprintNodeSetImageOpacity>,
        BlueprintSources.ICloneable {}

    [Serializable]
    [BlueprintNode(Name = "Set UI element opacity", Category = "UI", Color = BlueprintColors.Node.Actions)]
    public struct BlueprintNodeSetImageOpacity : IBlueprintNode, IBlueprintEnter {

        [SerializeField] private float _value;
        [SerializeField] private Graphic _graphic;

        private Color _color;

        public void OnStart(IBlueprint blueprint, NodeToken token) {
            _graphic = blueprint.Read(token, 2, _graphic);
            _color = _graphic.color;
        }
        
        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Enter("Set"));
            meta.AddPort(id, Port.Input<float>());
            meta.AddPort(id, Port.Input<Graphic>());
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            if (port != 0) {
                return;
            }

            _value = blueprint.Read(token, 1, _value);
            _graphic = blueprint.Read(token, 2, _graphic);
            _color.a = _value;
            _graphic.color = _color;
        }
    }

}