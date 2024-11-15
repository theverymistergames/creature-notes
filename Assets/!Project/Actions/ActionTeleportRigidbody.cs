using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using UnityEngine;

namespace MisterGames.Character.Motion {
    
    [Serializable]
    public sealed class ActionTeleportRigidbody : IActorAction {
        
        public Rigidbody body;
        public Transform transform;
        public bool resetVelocity = true;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            if (resetVelocity) {
                body.angularVelocity = Vector3.zero;
                body.linearVelocity = Vector3.zero;
            }
            
            body.isKinematic = true;
            var interpolation = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            
            transform.GetPositionAndRotation(out var pos, out var rot);
            
            var t = body.transform;
            t.SetPositionAndRotation(pos, rot);
            
            body.isKinematic = false;
            body.interpolation = interpolation;

            return default;
        }
    }
    
}