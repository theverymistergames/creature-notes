using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace MisterGames.ActionLib.GameObjects {

    [Serializable]
    public sealed class AwaitMonsterFinishedAction : IActorAction {
        public ForestMonster monster;
        private bool _stopped;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            monster.Stopped += OnMonsterStopped;
            return WaitForMonsterStopped(cancellationToken);
        }

        private void OnMonsterStopped() {
            _stopped = true;
            monster.Stopped -= OnMonsterStopped;
        }

        private async UniTask WaitForMonsterStopped(CancellationToken cancellationToken) {
            await UniTask.WaitUntil(() => _stopped, cancellationToken: cancellationToken);
        }
    }
    
}