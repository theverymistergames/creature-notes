using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MisterGames.ActionLib.Time {
    
    [Serializable]
    public sealed class ActionRepeatWithDelay : IActorAction {

        public float delayMin = 0f;
        public float delayMax = 1f;
        
        [SerializeReference] [SubclassSelector] public IActorAction action;
        
        public async UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            while (!cancellationToken.IsCancellationRequested) {
                float delay = Random.Range(delayMin, delayMax);
                
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();
                
                if (cancellationToken.IsCancellationRequested) return;
                
                await action.Apply(context, cancellationToken);
            }
        }
    }
    
}