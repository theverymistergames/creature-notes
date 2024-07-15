using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Collisions;
using MisterGames.Character.Motion;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MisterGames.ActionLib.GameObjects {
    [Serializable]
    public sealed class ActionSetEulerAngles : IActorAction {
        public Transform transform;
        public Vector3 eulerAngles;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            transform.localEulerAngles = eulerAngles;
            return default;
        }
    }
    
}