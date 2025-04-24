using System;
using MisterGames.Common.GameObjects;
using MisterGames.Dbg.Console.Attributes;
using MisterGames.Dbg.Console.Core;
using MisterGames.Input.Bindings;
using Object = UnityEngine.Object;

namespace _Project.Scripts.Runtime.Enemies {
    
    [Serializable]
    public class MonsterSpawnerConsoleModule : IConsoleModule {
        
        public ConsoleRunner ConsoleRunner { get; set; }
        
        [ConsoleCommand("monsters/start")]
        [ConsoleCommandHelp("start monster spawner with current level or default config")]
        public void StartSpawn() {
            if (!TryGetMonsterSpawner(out var spawner)) {
                ConsoleRunner.AppendLine($"MonsterSpawner not found");
                return;
            }

            if (!spawner.TryGetConfigForCurrentLevel(out var config) && 
                !spawner.TryGetCurrentConfig(out config)) 
            {
                ConsoleRunner.AppendLine($"MonsterSpawnerConfigs not found in MonsterSpawner at {spawner.GetPathInScene()}");
                return;
            }

            ConsoleRunner.AppendLine($"Start monsters spawning at {spawner.GetPathInScene()}, config {config.name}");
            spawner.StartSpawning(config, resetFlesh: true);
        }
        
        [ConsoleCommand("monsters/continue")]
        [ConsoleCommandHelp("continue monster spawner with current config")]
        public void ContinueSpawn() {
            if (!TryGetMonsterSpawner(out var spawner)) {
                ConsoleRunner.AppendLine($"MonsterSpawner not found");
                return;
            }

            if (!spawner.TryGetCurrentConfig(out var config)) 
            {
                ConsoleRunner.AppendLine($"No current MonsterSpawnerConfig is set in MonsterSpawner at {spawner.GetPathInScene()}");
                return;
            }

            ConsoleRunner.AppendLine($"Continue monsters spawning at {spawner.GetPathInScene()}, config {config.name}");
            spawner.ContinueSpawningFromCompletedWaves(resetFlesh: false);
        }

        [ConsoleCommand("monsters/stop")]
        [ConsoleCommandHelp("stop monster spawner")]
        public void StopSpawn() {
            if (!TryGetMonsterSpawner(out var spawner)) {
                ConsoleRunner.AppendLine($"MonsterSpawner not found");
                return;
            }
            
            if (!spawner.TryGetCurrentConfig(out var config)) 
            {
                ConsoleRunner.AppendLine($"No current MonsterSpawnerConfig is set in MonsterSpawner at {spawner.GetPathInScene()}");
                return;
            }

            ConsoleRunner.AppendLine($"Stop monsters spawning at {spawner.GetPathInScene()}");
            spawner.StopSpawning(resetFlesh: true);
        }

        [ConsoleCommand("monsters/toggle")]
        [ConsoleHotkey("monsters/toggle", KeyBinding.M, ShortcutModifiers.Shift)]
        [ConsoleCommandHelp("start, continue or stop monster spawner")]
        public void ToggleSpawn() {
            if (!TryGetMonsterSpawner(out var spawner)) {
                ConsoleRunner.AppendLine($"MonsterSpawner not found");
                return;
            }

            if (spawner.IsBattleRunning) {
                StopSpawn();
            }
            else {
                ContinueSpawn();
            }
        }

        private static bool TryGetMonsterSpawner(out MonsterSpawner spawner) {
            spawner = Object.FindFirstObjectByType<MonsterSpawner>();
            return spawner != null;
        }
    }
    
}