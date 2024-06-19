using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Collisions;
using MisterGames.Character.Inventory;
using MisterGames.Character.Motion;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace MisterGames.ActionLib.GameObjects {
    [Serializable]
    public sealed class ActionCharacterTeleport : IActorAction {
        public Transform point;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            var collisions = context.GetComponent<CharacterCollisionPipeline>();
            
            collisions.enabled = false;
            context.GetComponent<CharacterBodyAdapter>().Position = point.position;
            collisions.enabled = true;
            
            return default;
        }
    }
    
}