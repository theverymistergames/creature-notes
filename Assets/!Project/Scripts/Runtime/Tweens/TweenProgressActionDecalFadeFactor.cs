using System;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace MisterGames.TweenLib {
    [Serializable]
    public sealed class TweenProgressActionDecalFadeFactor : ITweenProgressAction {
        public DecalProjector projector;
        public Vector2 fromTo;

        public void OnProgressUpdate(float progress) {
            projector.fadeFactor = Mathf.Lerp(fromTo.x, fromTo.y, progress);
        }
    }
}
