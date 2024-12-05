using System;
using MisterGames.Blueprints;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    [BlueprintNode(Name = "Event Extended", Category = "Scenario", Color = BlueprintColors.Node.Events)]
    public sealed class BlueprintNodeEventExtended : 
        IBlueprintNode, 
        IBlueprintEnter, 
        IEventListener, 
        IBlueprintOutput<int>,
        IBlueprintOutput<bool>
    {

        [SerializeField] private EventReference _event;

        private IBlueprint _blueprint;
        private NodeToken _token;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Enter("Set")); 
            meta.AddPort(id, Port.Enter("Raise")); 
            meta.AddPort(id, Port.Input<EventReference>("Event"));
            meta.AddPort(id, Port.Exit("On Raised"));
            meta.AddPort(id, Port.Output<int>("Raise Count"));
            meta.AddPort(id, Port.Output<bool>("Raised Once"));
            meta.AddPort(id, Port.Input<int>("SubId"));
        }

        public void OnInitialize(IBlueprint blueprint, NodeToken token, NodeId root) {
            _blueprint = blueprint;
            _token = token;
            _event.Subscribe(this);
        }

        public void OnDeInitialize(IBlueprint blueprint, NodeToken token, NodeId root) {
            _blueprint = null;
            _event.Unsubscribe(this);
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            _token = token;
            
            switch (port) {
                case 0:
                    _event = blueprint
                        .Read(token, 2, _event)
                        .WithSubId(blueprint.Read(token, 6, _event.SubId));
                    
                    _event.Subscribe(this);
                    break;
                case 1:
                    _event.Raise();
                    break;
            }
        }

        int IBlueprintOutput<int>.GetPortValue(IBlueprint blueprint, NodeToken token, int port) {
            return port == 4 ? _event.GetCount() : default;
        }

        bool IBlueprintOutput<bool>.GetPortValue(IBlueprint blueprint, NodeToken token, int port) {
            return port == 5 ? _event.GetCount() > 0 : default;
        }

        public void OnEventRaised(EventReference e) {
            _blueprint.Call(_token, 3);
        }
    }

}
