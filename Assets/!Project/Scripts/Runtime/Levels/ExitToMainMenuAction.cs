using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Attributes;
using UnityEngine;

namespace _Project.Scripts.Runtime.Levels {
    
    [Serializable]
    public sealed class ExitToMainMenuAction : IActorAction {

        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            return LevelService.Instance?.ExitToMainMenu() ?? default;
        }
    }
    
}