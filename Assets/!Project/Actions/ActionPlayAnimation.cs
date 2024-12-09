using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Events;

namespace MisterGames.ActionLib.GameObjects {
    [Serializable]
    public sealed class ActionPlayAnimation : IActorAction {
        public Animator animator;
        public string animationName;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            animator.Play(animationName);
            return default;
        }
    }
    
}