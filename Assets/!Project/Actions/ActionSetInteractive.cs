using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Interact.Interactives;

namespace MisterGames.ActionLib.GameObjects {
    
    [Serializable]
    public sealed class ActionSetInteractive : IActorAction {
        
        public Interactive[] _interactives;
        public bool isInteractive;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            for (int i = 0; i < _interactives.Length; i++) {
                _interactives[i].enabled = isInteractive;
            }

            return default;
        }
    }
    
}