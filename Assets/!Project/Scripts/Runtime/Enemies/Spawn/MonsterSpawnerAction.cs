using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Attributes;

namespace _Project.Scripts.Runtime.Enemies {
    
    [Serializable]
    public sealed class MonsterSpawnerAction : IActorAction {

        public MonsterSpawner spawner;
        public Operation operation;
        [VisibleIf(nameof(operation), value: 0)]
        public MonsterSpawnerConfig config;
        
        public enum Operation {
            Start,
            ContinueCompletedWaves,
            Stop,
        }
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            switch (operation) {
                case Operation.Start:
                    spawner.StartSpawning(config);
                    break;
                
                case Operation.ContinueCompletedWaves:
                    spawner.ContinueSpawningFromCompletedWaves();
                    break;
                
                case Operation.Stop:
                    spawner.StopSpawning();
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return default;
        }
    }
    
}