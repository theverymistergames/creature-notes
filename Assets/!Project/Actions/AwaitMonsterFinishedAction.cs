using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;

namespace MisterGames.ActionLib.GameObjects {

    [Serializable]
    public sealed class AwaitMonsterFinishedAction : IActorAction {
        
        public ForestMonster monster;
        
        public async UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            while (!cancellationToken.IsCancellationRequested && !monster.IsStopped) {
                await UniTask.Yield();
            }
        }
    }
    
}