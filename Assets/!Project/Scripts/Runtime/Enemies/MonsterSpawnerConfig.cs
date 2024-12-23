using System;
using MisterGames.Common.Attributes;
using MisterGames.Common.Labels;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {

    [CreateAssetMenu(fileName = nameof(MonsterSpawnerConfig), menuName = "Monsters/" + nameof(MonsterSpawnerConfig))]
    public sealed class MonsterSpawnerConfig : ScriptableObject{
        
        [Header("Events")]
        public EventReference startedWaveEvent;
        public EventReference completedWavesCounter;
        public EventReference monsterKilledEvent;
        public EventReference killCharacterEvent;

        [Header("Flesh")]
        [Range(0f, 1f)] public float killCharacterAtFleshProgress = 0.99f;
        [Min(0f)] public float fleshProgressSmoothing = 1f;
        
        [Header("Waves")]
        public MonsterWave[] monsterWaves;
        
        [Serializable]
        public struct MonsterWave {
            [Min(0f)] public float startDelay;
            [MinMaxSlider(0f, 100f)] public Vector2 respawnDelayStart;
            [MinMaxSlider(0f, 100f)] public Vector2 respawnDelayEnd;
            [Min(-1)] public int killsToCompleteWave;
            [Min(-1)] public int maxAliveMonstersAtMoment;
            [Min(0)] public int armedMonstersToKillCharacter;
            public MonsterPreset[] monsterPresets;
            public SpawnExceptionGroup[] disallowSpawnTogether;
        }
        
        [Serializable]
        public struct MonsterPreset {
            public LabelValue[] monsterIds;
            [Min(0)] public int maxMonstersAtMoment;
            [Min(0)] public int allowSpawnMinKills;
            [Min(0f)] public float allowSpawnDelayAfterWaveStart;
            [Min(0f)] public float respawnCooldownAfterKill;
            [MinMaxSlider(0f, 100f)] public Vector2 armDurationStart;
            [MinMaxSlider(0f, 100f)] public Vector2 armDurationEnd;
            [MinMaxSlider(0f, 100f)] public Vector2 attackCooldownStart;
            [MinMaxSlider(0f, 100f)] public Vector2 attackCooldownEnd;
        }

        [Serializable]
        public struct SpawnExceptionGroup {
            public LabelValue[] theseMonsters;
            public LabelValue[] withTheseMonsters;
        }
    }
    
}