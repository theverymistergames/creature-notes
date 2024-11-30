using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Async;
using MisterGames.Common.Lists;
using MisterGames.Common.Maths;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterSpawner : MonoBehaviour {

        [SerializeField] private MonsterSpawnerConfig _config;

        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo;
        
        private readonly HashSet<Monster> _aliveMonsters = new();
        private readonly HashSet<Monster> _armedMonsters = new();
        private MonsterSpawnerConfig.MonsterPreset[] _monsterPresetsCache;
        
        private CancellationTokenSource _enableCts;
        private byte _spawnProcessId;
        private float _waveStartTime;
        private float _nextSpawnTime;
        
        private int _currentWave;
        private int _currentWaveKills;
        private int _totalKills;

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            StartSpawning();
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            StopSpawning();
        }

        private void StartSpawning() {
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: start spawning, " +
                                          $"waves completed {_config.completedWavesCounter.GetRaiseCount()}/{_config.monsterWaves.Length}.");
#endif

            _currentWave = -1;
            _totalKills = 0;
            _currentWaveKills = 0;
            
            KillAllMonsters(notifyDamage: false);
            StartSpawningAsync(_enableCts.Token).Forget();
        }

        private void StopSpawning() {
            _spawnProcessId++;
            
            DisposeMonsterPresetsCache();
            KillAllMonsters(notifyDamage: false);
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: stopped spawning, " +
                                          $"waves completed {_config.completedWavesCounter.GetRaiseCount()}/{_config.monsterWaves.Length}.");
#endif
        }

        private async UniTask StartSpawningAsync(CancellationToken cancellationToken) {
            byte id = ++_spawnProcessId;

            while (id == _spawnProcessId && !cancellationToken.IsCancellationRequested) {
                if (TryFinishWave(ref _currentWave)) {
                    KillAllMonsters(notifyDamage: true);
                    
                    if (_currentWave >= _config.monsterWaves.Length) break;
                    
                    var wave = _config.monsterWaves[_currentWave];
                    
#if UNITY_EDITOR
                    if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: starting wave {_currentWave} in {wave.startDelay} s.");
#endif
                    
                    await UniTask.Delay(TimeSpan.FromSeconds(wave.startDelay), cancellationToken: cancellationToken)
                        .SuppressCancellationThrow();
         
                    if (id != _spawnProcessId || cancellationToken.IsCancellationRequested) break;

#if UNITY_EDITOR
                    if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: started wave {_currentWave}.");
#endif
                    
                    _config.startedWaveEvent.Raise();
                    _waveStartTime = Time.time;
                    
                    RecreateMonsterPresetsCache(_currentWave);
                }
                
                CheckAliveMonsters(_currentWave);
                CheckSpawns(_currentWave, _waveStartTime);

                await UniTask.Yield();
            }
        }
        
        private bool TryFinishWave(ref int waveIndex) {
            int completedWaves = _config.completedWavesCounter.GetRaiseCount();
            
            if (waveIndex < 0 || waveIndex >= _config.monsterWaves.Length) {
                waveIndex = completedWaves;
                _currentWaveKills = 0;
                
#if UNITY_EDITOR
                if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: {completedWaves} completed waves, set wave index {waveIndex}.");
#endif
                return true;
            }

            ref var wave = ref _config.monsterWaves[waveIndex];
            if (wave.killsToCompleteWave < 0 || _currentWaveKills < wave.killsToCompleteWave) {
                return false;
            }
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: completed wave {waveIndex}, " +
                                          $"kills per wave {_currentWaveKills}/{wave.killsToCompleteWave}, " +
                                          $"kills total {_totalKills}.");
#endif

            _currentWaveKills = 0;
            _config.completedWavesCounter.SetCount(++waveIndex);
            return true;
        }

        private void CheckAliveMonsters(int waveIndex) {
            ref var wave = ref _config.monsterWaves[waveIndex];
            
            for (int i = 0; i < wave.monsterPresets.Length; i++) {
                ref var preset = ref wave.monsterPresets[i];
                if (!_aliveMonsters.Contains(preset.monster)) continue;
                
                if (preset.monster.IsDead) {
                    _aliveMonsters.Remove(preset.monster);
                    _armedMonsters.Remove(preset.monster);

                    _currentWaveKills++;
                    _totalKills++;
                    _config.monsterKilledEvent.Raise();
                    
#if UNITY_EDITOR
                    if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, killed monster [{preset.monster}]. " +
                                                  $"Kills per wave {_currentWaveKills}/{wave.killsToCompleteWave}, " +
                                                  $"kills total {_totalKills}, " +
                                                  $"alive monsters per wave {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}.");
#endif
                    
                    continue;
                }

                if (preset.monster.IsArmed && _armedMonsters.Add(preset.monster)) {
#if UNITY_EDITOR
                    if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, armed monster [{preset.monster}]. " +
                                                  $"Alive monsters per wave {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}, " +
                                                  $"armed monsters {_armedMonsters.Count}.");
#endif
                    continue;
                }
                
                if (!preset.monster.IsArmed && _armedMonsters.Contains(preset.monster)) {
                    _armedMonsters.Remove(preset.monster);
                }
            }
        }

        private void CheckSpawns(int waveIndex, float waveStartTime) {
            ref var wave = ref _config.monsterWaves[waveIndex];
            float time = Time.time;

            if (time < _nextSpawnTime ||
                wave.maxAliveMonstersAtMoment >= 0 && _aliveMonsters.Count >= wave.maxAliveMonstersAtMoment
            ) {
                return;
            }
            
            _monsterPresetsCache.Shuffle();

            for (int i = 0; i < _monsterPresetsCache.Length; i++) {
                ref var preset = ref wave.monsterPresets[i];
                
                if (!preset.monster.IsDead ||
                    time < waveStartTime + preset.allowSpawnDelay ||
                    _currentWaveKills < preset.minKillsToAllowSpawn
                ) {
                    continue;
                }
                
                float t = wave.killsToCompleteWave > 0 ? (float) _currentWaveKills / wave.killsToCompleteWave : 0f;
                
                float respawnDelay = Mathf.Lerp(
                    wave.respawnDelayStart.GetRandomInRange(),
                    wave.respawnDelayEnd.GetRandomInRange(),
                    t
                );
                
                float armDuration = Mathf.Lerp(
                    preset.armDurationStart.GetRandomInRange(),
                    preset.armDurationEnd.GetRandomInRange(),
                    t
                );

                var attackCooldownRange = new Vector2(
                    Mathf.Lerp(preset.attackCooldownStart.x, preset.attackCooldownEnd.x, t), 
                    Mathf.Lerp(preset.attackCooldownStart.y, preset.attackCooldownEnd.y, t)
                );
                
                preset.monster.Respawn(armDuration, attackCooldownRange);
                
                _aliveMonsters.Add(preset.monster);
                _nextSpawnTime = time + respawnDelay;
                
#if UNITY_EDITOR
                if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, respawned monster [{preset.monster}] with arm duration {armDuration} s. " +
                                              $"Next respawn available in {respawnDelay} s. " +
                                              $"Alive monsters per wave {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}.");
#endif
                
                return;
            }
        }

        private void KillAllMonsters(bool notifyDamage = true) {
            _nextSpawnTime = 0f;
            
            foreach (var monster in _aliveMonsters) {
                monster.Kill(notifyDamage);
            }
            
            _armedMonsters.Clear();
            _aliveMonsters.Clear();
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: killed all monsters.");
#endif
        }

        private void RecreateMonsterPresetsCache(int waveIndex) {
            DisposeMonsterPresetsCache();
            
            ref var wave = ref _config.monsterWaves[waveIndex];

            _monsterPresetsCache = ArrayPool<MonsterSpawnerConfig.MonsterPreset>.Shared.Rent(wave.monsterPresets.Length);
            Array.Copy(wave.monsterPresets, _monsterPresetsCache, _monsterPresetsCache.Length);
        }

        private void DisposeMonsterPresetsCache() {
            if (_monsterPresetsCache == null) return;
            
            ArrayPool<MonsterSpawnerConfig.MonsterPreset>.Shared.Return(_monsterPresetsCache);
            _monsterPresetsCache = null;
        }
    }
    
}