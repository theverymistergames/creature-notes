using System;
using MisterGames.Common.GameObjects;
using UnityEngine;

namespace _Project.Scripts.Runtime.Telescope {

    [CreateAssetMenu(fileName = nameof(StarGroupsData), menuName = "MisterGames/Telescope" + nameof(StarGroupsData))]
    public sealed class StarGroupsData : ScriptableObject {
        
        [Header("Detection Settings")]
        [Min(0f)] public float hoverAngle;
        [Min(0f)] public float detectionAngle;
        [Min(0f)] public Vector2 detectionFovRange;
        [Min(0f)] public float detectionTime;
        [Min(0f)] public float takeLensOffDelayAfterDetection = 1f;

        [Header("Emission Settings")]
        [ColorUsage(true, true)]
        public Color emissionNormal;
        [ColorUsage(true, true)]
        public Color emissionHover;
        [ColorUsage(true, true)]
        public Color emissionDetected;
        public float emissionFrequencyHover;
        public float emissionFrequencyDetected;
        public float emissionRangeHover;
        public float emissionRangeDetected;
        [Min(0f)] public float emissionSmoothing = 10f;
        
        [Header("Visual Settings")]
        public Transform starPrefab;
        public float starScale;
        public float groupScale;
        public float telescopeDistance;
        public StarGroup[] starGroups;

        [Serializable]
        public struct StarGroup {
            [Header("Canvas Settings")]
            public Vector3 canvasRotation;
            public float canvasScale;
            public TransformData[] stars;
            public TransformData[] links;

            [Header("Placement Settings")]
            public Transform lensPrefab;
            public Vector3 telescopeOrientation;
        }
    }
    
}