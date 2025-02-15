using System;
using Deform;
using MisterGames.Tweens;
using UnityEngine;

namespace MisterGames.TweenLib.Deform {
    
    [Serializable]
    public sealed class DeformFactorTweenProgressAction : ITweenProgressAction {

        public Deformer deformer;
        public float startFactor;
        public float endFactor;
        
        public void OnProgressUpdate(float progress) {
            if (deformer is IFactor factor) factor.Factor = Mathf.Lerp(startFactor, endFactor, progress);
            
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(deformer);
#endif
        }
    }
    
}