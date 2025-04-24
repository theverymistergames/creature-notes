using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;

namespace _Project.Scripts.Runtime.Spider {
    
    [Serializable]
    public sealed class BurnSpiderWebAction : IActorAction {

        public SpiderWebPlacer spiderWebPlacer;
        public bool instant;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            spiderWebPlacer.BurnWeb(instant);
            
            return default;
        }
    }
    
}