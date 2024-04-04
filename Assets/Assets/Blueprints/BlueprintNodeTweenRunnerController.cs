using System;
using MisterGames.Blueprints;
using MisterGames.Blueprints.Nodes;
using MisterGames.Interact.Interactives;
using MisterGames.Tweens;
using UnityEngine;

namespace MisterGames.BlueprintLib {

    [Serializable]
    [BlueprintNode(Name = "Tween Runner Controller", Category = "Tweens", Color = BlueprintColors.Node.Actions)]
    public sealed class BlueprintNodeTweenRunnerController :
        IBlueprintNode,
        IBlueprintEnter,
        IBlueprintStartCallback
    {
        // [SerializeField] private bool _autoSetInteractiveOnStart = true;

        private TweenRunner _runner;
        private IBlueprint _blueprint;
        private NodeToken _token;

        public void CreatePorts(IBlueprintMeta meta, NodeId id) {
            meta.AddPort(id, Port.Input<TweenRunner>("Tween Runner"));
            meta.AddPort(id, Port.Enter("Play"));
        }

        public void OnStart(IBlueprint blueprint, NodeToken token) {
            // if (!_autoSetInteractiveOnStart) return;

            _token = token;
            _blueprint = blueprint;
        }

        public void OnEnterPort(IBlueprint blueprint, NodeToken token, int port) {
            if (port != 1) return;

            _token = token;
            _runner = _blueprint.Read(_token, 0, _runner);
            _runner.TweenPlayer.Play();
        }
    }

}
