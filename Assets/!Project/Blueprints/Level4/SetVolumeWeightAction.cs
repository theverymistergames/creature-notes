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
    public sealed class SetVolumeWeightAction : IActorAction {

        public PlayerLoopStage playerLoopStage = PlayerLoopStage.Update;
        public Volume volume;
        public float duration;
        public bool inverted = false;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            SetWeight(cancellationToken);
            return default;
        }

        private async void SetWeight(CancellationToken cancellationToken) {
            var progress = 0f;
            var timeSource = TimeSources.Get(playerLoopStage);
            
            if (!inverted) volume.gameObject.SetActive(true);
            
            while (!cancellationToken.IsCancellationRequested) {
                float progressDelta = timeSource.DeltaTime / duration;
                progress = Mathf.Clamp01(progress + progressDelta);

                volume.weight = inverted ? 1 - progress : progress;
                if (progress >= 1f) break;

                await UniTask.Yield();
            }
            
            if (inverted) volume.gameObject.SetActive(false);
        }
    }
    
}