﻿using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;

namespace _Project.Scripts.Runtime.Enemies.Bed {
    
    [Serializable]
    public sealed class StopGravityAttackAction : IActorAction {
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            if (context.TryGetComponent(out GravityAttackBehaviour gravityAttackBehaviour)) {
                gravityAttackBehaviour.StopAntiGravity();
            }
            
            return default;
        }
    }
    
}