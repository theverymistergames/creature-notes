using System;
using MisterGames.Tweens;

namespace _Project.Scripts.Runtime.Spider {
    
    [Serializable]
    public sealed class SpiderWebPlacerProgressTweenAction : ITweenProgressAction {

        public SpiderWebPlacer spiderWebPlacer;
        
        public void OnProgressUpdate(float progress) {
            spiderWebPlacer.SetSpawnProgressManual(progress);
        }
    }
    
}