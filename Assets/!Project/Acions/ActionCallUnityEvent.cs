using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Inventory;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

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