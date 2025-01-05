using System;
using Deform;
using MisterGames.Tweens;
using UnityEngine;

namespace MisterGames.TweenLib.Deform {
    
    [Serializable]
    public sealed class BendDeformAngleTweenProgressAction : ITweenProgressAction {

        public BendDeformer bendDeformer;
        public float startAngle;
        public float endAngle;
        
        public void OnProgressUpdate(float progress) {
            bendDeformer.Angle = Mathf.Lerp(startAngle, endAngle, progress);

#if UNITY_EDITOR
            if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(bendDeformer);
#endif
        }
    }
    
}