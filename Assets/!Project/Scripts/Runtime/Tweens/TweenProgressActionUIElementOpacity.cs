using System;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

namespace MisterGames.TweenLib {
    [Serializable]
    public sealed class TweenProgressActionUIElementOpacity : ITweenProgressAction {
        public Vector2 fromTo;
        public Graphic graphic;

        public void OnProgressUpdate(float progress) {
            var color = graphic.color;
            
            color.a = Mathf.Lerp(fromTo.x, fromTo.y, progress);;
            
            graphic.color = color;
        }
    }
}
