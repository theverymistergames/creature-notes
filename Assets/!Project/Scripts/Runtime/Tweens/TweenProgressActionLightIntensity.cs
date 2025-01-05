using System;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace MisterGames.TweenLib {

    [Serializable]
    public sealed class TweenProgressActionLightIntensity : ITweenProgressAction {

        public HDAdditionalLightData light;
        public Vector2 intensity;

        public void OnProgressUpdate(float progress) {
            light.intensity = Mathf.Lerp(intensity.x, intensity.y, progress);
        }
    }

}
