using System;
using Deform;
using MisterGames.Tweens;
using UnityEngine;

namespace MisterGames.TweenLib.Deform {
    
    [Serializable]
    public sealed class BendDeformFactorTweenProgressAction : ITweenProgressAction {

        public BendDeformer bendDeformer;
        public float startFactor;
        public float endFactor;
        
        public void OnProgressUpdate(float progress) {
            bendDeformer.Factor = Mathf.Lerp(startFactor, endFactor, progress);
            
#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(bendDeformer);
#endif
        }
    }
    
}