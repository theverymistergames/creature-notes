using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    [Serializable]
    public sealed class ApplyDebuffAction : IActorAction {
    
        public EventReference debuffEvent;
        public Sprite sprite;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            debuffEvent.Raise(sprite);
            return default;
        }
    }
    
}