using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using UnityEngine;

namespace MisterGames.ActionLib.GameObjects {
 
    [Serializable]
    public sealed class ActionSetPosition : IActorAction {
        
        public Transform transform;
        public Mode mode;
        public Vector3 position;
        
        public enum Mode {
            Local,
            World,
        }
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            switch (mode) {
                case Mode.Local:
                    transform.localPosition = position;
                    break;
                
                case Mode.World:
                    transform.position = position;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return default;
        }
    }
    
}