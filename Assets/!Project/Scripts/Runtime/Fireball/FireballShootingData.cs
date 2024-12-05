using System;
using MisterGames.Actors;
using UnityEngine;

namespace _Project.Scripts.Runtime.Fireball {
    
    [Serializable]
    public sealed class FireballShootingData : IActorData {
        
        [Header("Stages")]
        [Min(0f)] public float noneDuration;
        [Min(0f)] public float prepareDuration;
        [Min(0f)] public float chargeDuration;
        [Min(0f)] public float cooldownDuration;
        [Min(0f)] public float overheatDuration;
        [Min(0f)] public float overheatCooldownDuration;

        [Header("Shot")]
        public Actor shotPrefab;
        public Vector3 spawnOffset;
        [Min(0f)] public float forceStart;
        [Min(0f)] public float forceEnd;
        [Range(-90f, 90f)] public float angleStart;
        [Range(-90f, 90f)] public float angleEnd;
        public AnimationCurve forceByChargeProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve angleByChargeProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }
    
}