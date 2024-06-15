using System;
using MisterGames.Common.GameObjects;
using UnityEngine;

namespace _Project.Scripts.Runtime.Telescope {

    [CreateAssetMenu(fileName = nameof(StarGroupsData), menuName = "MisterGames/Telescope" + nameof(StarGroupsData))]
    public sealed class StarGroupsData : ScriptableObject {
        
        public Transform starPrefab;
        [Min(0f)] public float detectionAngle;
        [Min(0f)] public float detectionTime;
        public StarGroup[] starGroups;

        [Serializable]
        public struct StarGroup {
            [Header("Canvas Settings")]
            public Vector3 canvasRotation;
            public float canvasScale;
            public TransformData[] stars;
            public TransformData[] links;

            [Header("Placement Settings")]
            public float starScale;
            public float groupScale;
            public float telescopeDistance;
            public Vector3 telescopeOrientation;
        }
    }
    
}