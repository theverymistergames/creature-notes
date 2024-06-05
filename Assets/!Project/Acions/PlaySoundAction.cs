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
    public sealed class PlaySoundAction : IActorAction {
        public AudioSource source;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            source.Play();
            return default;
        }
    }
    
}