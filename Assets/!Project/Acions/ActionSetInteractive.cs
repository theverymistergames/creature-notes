using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Inventory;
using MisterGames.Interact.Interactives;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace MisterGames.ActionLib.GameObjects {
    [Serializable]
    public sealed class ActionSetInteractive : IActorAction {
        public Interactive[] _interactives;
        public bool isInteractive;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            foreach (var interactive in _interactives) {
                interactive.enabled = isInteractive;
            }
            return default;
        }
    }
    
}