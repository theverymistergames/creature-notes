using UnityEngine;

namespace _Project.Scripts.Runtime.Spider {
    
    public interface ISpiderWebPlacer {

        Material GetMaterial();

        void NotifyBurn();
    }
    
}