using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Attributes;
using MisterGames.Tick.Core;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace MisterGames.ActionLib.Time {
    [Serializable]
    public sealed class ActionRepeatWithDelay : IActorAction {

        public float delayMin = 0f;
        public float delayMax = 1f;
        
        [SerializeReference] [SubclassSelector] public IActorAction action;
        
        public async UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            while (true) {
                var delay = delayMin + (delayMax - delayMin) * Random.Range(0f, 1f);
                await UniTask.Delay((int)(delay * 1000));
                
                await action.Apply(context, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested) break;
            }
        }
    }
    
}