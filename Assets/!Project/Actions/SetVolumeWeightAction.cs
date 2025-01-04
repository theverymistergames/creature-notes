using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Tick;
using UnityEngine;
using UnityEngine.Rendering;

namespace MisterGames.ActionLib.GameObjects {

    [Serializable]
    public sealed class SetVolumeWeightAction : IActorAction {

        public Volume volume;
        public float duration;
        public bool inverted;
        public bool wait;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            if (wait) return SetWeight(cancellationToken);
            
            SetWeight(cancellationToken).Forget();
            return default;
        }

        private async UniTask SetWeight(CancellationToken cancellationToken) {
            if (!inverted) volume.gameObject.SetActive(true);
            
            var timeSource = PlayerLoopStage.Update.Get();
            float progress = 0f;
            
            while (!cancellationToken.IsCancellationRequested) {
                progress = Mathf.Clamp01(progress + timeSource.DeltaTime / duration);

                volume.weight = inverted ? 1f - progress : progress;
                if (progress >= 1f) break;

                await UniTask.Yield();
            }

            if (cancellationToken.IsCancellationRequested) return;
            
            if (inverted) volume.gameObject.SetActive(false);
        }
    }
    
}