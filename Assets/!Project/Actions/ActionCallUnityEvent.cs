using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using UnityEngine.Events;

namespace MisterGames.ActionLib.GameObjects {
    
    [Serializable]
    public sealed class ActionCallUnityEvent : IActorAction {
        public UnityEvent eventAction;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            eventAction?.Invoke();
            return default;
        }
    }
    
}