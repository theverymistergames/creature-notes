using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public abstract class FleshPositionSamplerBase : MonoBehaviour {

        public abstract bool TrySamplePosition(ref Vector3 point);
    }
    
}