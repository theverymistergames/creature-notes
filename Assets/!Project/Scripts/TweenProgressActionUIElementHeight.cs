using System;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace MisterGames.TweenLib {
    [Serializable]
    public sealed class TweenProgressActionUIElementHeight : ITweenProgressAction {
        public Vector2 fromTo;
        public RectTransform transform;

        public void OnProgressUpdate(float progress) {
            transform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Lerp(fromTo.x, fromTo.y, progress));
        }
    }
}
