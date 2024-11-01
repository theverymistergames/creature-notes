using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using UnityEngine;

namespace MisterGames.Character.Motion {
    [Serializable]
    public sealed class ActionTeleportRigidbody : IActorAction {
        [SerializeField] private Rigidbody body;
        [SerializeField] private Transform transform;

        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            body.angularVelocity = Vector3.zero;
            body.linearVelocity = Vector3.zero;
            
            body.rotation = transform.rotation;
            body.position = transform.position;

            return default;
        }
    }
}
