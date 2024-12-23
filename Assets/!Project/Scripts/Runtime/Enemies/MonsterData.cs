﻿using MisterGames.Actors;
using System;
using MisterGames.ActionLib.Sounds;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
	[Serializable]
    public sealed class MonsterData : IActorData {

        [Header("Attack")]
        public bool allowMultipleAttacks;
        [Min(0f)] public float attackDelay;
        
        [Header("Death")]
        [Min(0f)] public float deathDuration;
        
        [Header("Sounds")]
        public PlaySoundAction respawnSound;
        public PlaySoundAction deathSound;
        public PlaySoundAction armSound;
        public PlaySoundAction startAttackSound;
        public PlaySoundAction performAttackSound;
    }
    
}