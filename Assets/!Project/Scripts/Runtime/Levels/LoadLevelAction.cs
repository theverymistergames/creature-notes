using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Attributes;
using UnityEngine;

namespace _Project.Scripts.Runtime.Levels {
    
    [Serializable]
    public sealed class LoadLevelAction : IActorAction {

        public Mode mode;
        [VisibleIf(nameof(mode), 0)]
        [Min(0)] public int level;
        
        public enum Mode {
            Explicit,
            LastSaved,
        }
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            return mode switch {
                Mode.Explicit => LevelService.Instance?.LoadLevel(level) ?? default,
                Mode.LastSaved => LevelService.Instance?.LoadLastSavedLevel() ?? default,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
    
}