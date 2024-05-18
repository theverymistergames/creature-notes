using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Tick.Core;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace MisterGames.ActionLib.GameObjects {
    [Serializable]
    public sealed class PlayTweenRunnerAction : IActorAction {
        public TweenRunner runner;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            runner.TweenPlayer.Play(cancellationToken: cancellationToken, progress: 0);
            return default;
        }
    }
    
}