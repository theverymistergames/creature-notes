using MisterGames.Actors;
using System;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
	[Serializable]
    public sealed class MonsterData : IActorData {

        [Header("Attack")]
        public bool allowMultipleAttacks;
        [Min(0f)] public float attackDelay;
        
        [Header("Death")]
        [Min(0f)] public float deathDuration;
    }
    
}