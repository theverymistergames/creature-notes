using System;
using MisterGames.Tweens;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies.Lamp {
    
    [Serializable]
    public sealed class SetLampMonsterArmWeightProgressAction : ITweenProgressAction {

        public LampMonsterArmBehaviour lampMonsterArmBehaviour;
        [Range(0f, 1f)] public float startWeight;
        [Range(0f, 1f)] public float endWeight;
        
        public void OnProgressUpdate(float progress) {
            lampMonsterArmBehaviour.SetWeight(Mathf.Lerp(startWeight, endWeight, progress));
        }
    }
    
}