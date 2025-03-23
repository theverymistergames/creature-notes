using Deform;
using UnityEngine;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshBubble : MonoBehaviour {
        
        [SerializeField] private SpherifyDeformer _spherifyDeformer;
        [SerializeField] private SphereCollider _collider;
        
        public SphereCollider Collider => _collider;
        public SpherifyDeformer SpherifyDeformer => _spherifyDeformer;
        
    }
    
}