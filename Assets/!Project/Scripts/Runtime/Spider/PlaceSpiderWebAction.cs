using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Attributes;
using UnityEngine;

namespace _Project.Scripts.Runtime.Spider {
    
    [Serializable]
    public sealed class PlaceSpiderWebAction : IActorAction {

        public SpiderWebPlacer spiderWebPlacer;
        [MinMaxSlider(0f, 10f)] private Vector2 spawnDurationRange = new(1f, 2f);
        public PositionMode position;
        [VisibleIf(nameof(position), 2)]
        public Transform point;
        
        public enum PositionMode {
            Random,
            Actor,
            Explicit,
        }
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            var pos = position switch {
                PositionMode.Random => spiderWebPlacer.GetRandomSpawnPosition(),
                PositionMode.Actor => context.Transform.position,
                PositionMode.Explicit => point.position,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            spiderWebPlacer.PlaceWeb(pos, spawnDurationRange);
            
            return default;
        }
    }
    
}