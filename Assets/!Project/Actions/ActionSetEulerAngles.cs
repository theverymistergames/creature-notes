using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using UnityEngine;

namespace MisterGames.ActionLib.GameObjects {
    
    [Serializable]
    public sealed class ActionSetEulerAngles : IActorAction {
        
        public Transform transform;
        public Mode mode;
        public Vector3 eulerAngles;
        
        public enum Mode {
            Local,
            World,
        }
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            switch (mode) {
                case Mode.Local:
                    transform.localEulerAngles = eulerAngles;
                    break;
                
                case Mode.World:
                    transform.eulerAngles = eulerAngles;
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return default;
        }
    }
    
}