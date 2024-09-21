using System;
using MisterGames.Actors;
using UnityEngine;

namespace _Project.Scripts.Runtime.Fireball {
    
    [Serializable]
    public sealed class FireballShootingData : IActorData {
        
        [Header("Stages")]
        [Min(0f)] public float prepareDuration;
        [Min(0f)] public float chargeDuration;
        [Min(0f)] public float cooldownDuration;
        [Min(0f)] public float overheatDuration;
        [Min(0f)] public float overheatCooldownDuration;

        [Header("Shot")]
        public Rigidbody shotPrefab;
        public Vector3 spawnOffset;
        [Min(0f)] public float forceMin;
        [Min(0f)] public float forceMax;
        [Range(-90f, 90f)] public float angleMin;
        [Range(-90f, 90f)] public float angleMax;
        public AnimationCurve forceByChargeProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve angleByChargeProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }
    
}