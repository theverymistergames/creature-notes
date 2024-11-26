using System;
using MisterGames.Common.Attributes;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {

    [CreateAssetMenu(fileName = nameof(MonsterSpawnerConfig), menuName = "Monsters/" + nameof(MonsterSpawnerConfig))]
    public sealed class MonsterSpawnerConfig : ScriptableObject{
        
        [Header("Events")]
        public EventReference monsterRespawnedEvent;
        public EventReference monsterArmedEvent;
        public EventReference killedMonstersTotalCounter;
        public EventReference killedMonstersPerWaveCounter;
        public EventReference startedWavesCounter;
        public EventReference completedWavesCounter;
        
        [Header("Waves")]
        public MonsterWave[] monsterWaves;
        
        [Serializable]
        public struct MonsterWave {
            [Min(0f)] public float startDelay;
            [MinMaxSlider(0f, 100f)] public Vector2 respawnDelayStart;
            [MinMaxSlider(0f, 100f)] public Vector2 respawnDelayEnd;
            [Min(-1)] public int killsToCompleteWave;
            [Min(-1)] public int maxAliveMonstersAtMoment;
            public MonsterPreset[] monsterPresets;
        }
        
        [Serializable]
        public struct MonsterPreset {
            public Monster monster;
            [Min(0f)] public float allowSpawnDelay;
            [Min(0)] public int minKillsToAllowSpawn;
            [MinMaxSlider(0f, 100f)] public Vector2 armDurationStart;
            [MinMaxSlider(0f, 100f)] public Vector2 armDurationEnd;
            [MinMaxSlider(0f, 100f)] public Vector2 attackCooldownStart;
            [MinMaxSlider(0f, 100f)] public Vector2 attackCooldownEnd;
        }
    }
    
}