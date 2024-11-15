using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Data;
using MisterGames.Tweens;

namespace MisterGames.ActionLib.Tweens {
    
    [Serializable]
    public sealed class ResetTweenAction : IActorAction {
        
        public TweenRunner tweenRunner;
        public Optional<float> speed = new(value: 1f, hasValue: false);
        public Optional<float> progress = new(value: 0f, hasValue: false);
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            var tween = tweenRunner.TweenPlayer;
            
            if (speed.HasValue) tween.Speed = speed.Value;
            if (progress.HasValue) tween.Progress = progress.Value;

            return default;
        }
    }
    
}