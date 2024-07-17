using System;
using MisterGames.Tweens;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace MisterGames.TweenLib {

    [Serializable]
    public sealed class TweenProgressActionLightIntensity : ITweenProgressAction {

        public HDAdditionalLightData light;
        public Vector2 intensity;
        
        // public Renderer renderer;
        // public string fieldName;
        // public float startValue = 0;
        // public float endValue;
        // public bool useColor;

        // private Color _color;

        public void OnProgressUpdate(float progress) {
            light.intensity = Mathf.Lerp(intensity.x, intensity.y, progress);
            
//             var material = renderer.material;
//
//             if (material == renderer.sharedMaterial) {
//                 material = new Material(renderer.sharedMaterial);
//                 renderer.material = material;
//                 _color = material.color;
//             }
//
//             float value = Mathf.Lerp(startValue, endValue, progress);
//
//             if (useColor) {
//                 material.SetColor(fieldName, value * _color);
//             }
//             else {
//                 material.SetFloat(fieldName, value);
//             }
//
// #if UNITY_EDITOR
//             if (!Application.isPlaying && renderer != null) UnityEditor.EditorUtility.SetDirty(renderer);
// #endif
        }
    }

}
