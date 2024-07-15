using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Collisions;
using MisterGames.Character.Core;
using MisterGames.Character.Motion;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MisterGames.ActionLib.GameObjects {
    [Serializable]
    public sealed class ActionCharacterTeleport : IActorAction {
        public Transform point;
        public List<Transform> points = new();
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            var collisions = context.GetComponent<CharacterCollisionPipeline>();

            var finalPoint = points.Count > 0 ? points[Random.Range(0, points.Count)] : point;
            
            collisions.enabled = false;
            context.GetComponent<CharacterBodyAdapter>().Position = finalPoint.position;
            collisions.enabled = true;
            
            return default;
        }
    }
    
}