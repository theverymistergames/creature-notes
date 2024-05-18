using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace MisterGames.ActionLib.GameObjects {

    [Serializable]
    public sealed class LogAction : IActorAction {
        public string text = "log";
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            Debug.Log(text);
            return default;
        }
    }
    
}