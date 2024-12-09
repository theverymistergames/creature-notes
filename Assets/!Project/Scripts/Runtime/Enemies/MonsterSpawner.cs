using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Async;
using MisterGames.Common.Labels;
using MisterGames.Common.Lists;
using MisterGames.Common.Maths;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterSpawner : MonoBehaviour {

        [SerializeField] private MonsterSpawnerConfig _config;
        [SerializeField] private Monster[] _monsters;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo;
        
        private readonly Dictionary<int, Monster> _monstersMap = new();
        private readonly HashSet<int> _aliveMonsters = new();
        private readonly HashSet<int> _armedMonsters = new();
        private MonsterSpawnerConfig.MonsterPreset[] _monsterPresetsCache;
        private LabelValue[] _monsterIdsCache;
        
        private CancellationTokenSource _enableCts;
        private byte _spawnProcessId;
        private float _waveStartTime;
        private float _nextSpawnTime;
        
        private int _currentWave;
        private int _currentWaveKills;
        private int _totalKills;

        private void Awake() {
            FetchMonstersMap();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            StartSpawning();
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            StopSpawning();
        }

        private void FetchMonstersMap() {
            for (int i = 0; i < _monsters.Length; i++) {
                var monster = _monsters[i];
                _monstersMap.Add(monster.Id, monster);
            }
        }

        private void StartSpawning() {
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: start spawning, " +
                                          $"waves completed {_config.completedWavesCounter.GetCount()}/{_config.monsterWaves.Length}.");
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
            DisposeMonsterIdsCache();
            
            KillAllMonsters(notifyDamage: false);
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: stopped spawning, " +
                                          $"waves completed {_config.completedWavesCounter.GetCount()}/{_config.monsterWaves.Length}.");
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
                CheckSpawns(_currentWave);

                await UniTask.Yield();
            }
        }
        
        private bool TryFinishWave(ref int waveIndex) {
            int completedWaves = _config.completedWavesCounter.GetCount();
            
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

                for (int j = 0; j < preset.monsterIds.Length; j++) {
                    int monsterId = preset.monsterIds[j].value;
                    
                    if (!_aliveMonsters.Contains(monsterId) || !_monstersMap.TryGetValue(monsterId, out var monster)) {
                        continue;
                    }
                    
                    if (monster.IsDead) {
                        _aliveMonsters.Remove(monsterId);
                        _armedMonsters.Remove(monsterId);

                        _currentWaveKills++;
                        _totalKills++;
                        _config.monsterKilledEvent.Raise();
                    
#if UNITY_EDITOR
                        if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, killed monster [{preset.monsterIds[j]}]. " +
                                                      $"Kills per wave {_currentWaveKills}/{wave.killsToCompleteWave}, " +
                                                      $"kills total {_totalKills}, " +
                                                      $"alive monsters per wave {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}.");
#endif
                    
                        continue;
                    }

                    if (monster.IsArmed && _armedMonsters.Add(monsterId)) {
#if UNITY_EDITOR
                        if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, armed monster [{preset.monsterIds[j]}]. " +
                                                      $"Alive monsters per wave {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}, " +
                                                      $"armed monsters {_armedMonsters.Count}.");
#endif
                        continue;
                    }
                
                    if (!monster.IsArmed && _armedMonsters.Contains(monsterId)) {
                        _armedMonsters.Remove(monsterId);
                    }   
                }
            }
        }

        private void CheckSpawns(int waveIndex) {
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
                
                if (!CanSpawn(ref wave, ref preset, out int newMonsterId) || !_monstersMap.TryGetValue(newMonsterId, out var monster)) {
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
                
                monster.Respawn(armDuration, attackCooldownRange);
                _aliveMonsters.Add(newMonsterId);
                _nextSpawnTime = time + respawnDelay;
                
#if UNITY_EDITOR
                if (_showDebugInfo) {
                    preset.monsterIds.TryFind(newMonsterId, (l, id) => l.value == id, out var labelValue);
                    Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, respawned monster [{labelValue.GetLabel()}] with arm duration {armDuration} s. " +
                                              $"Next respawn available in {respawnDelay} s. " +
                                              $"Alive monsters per wave {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}.");
                    
                }
#endif
                
                return;
            }
        }

        private void KillAllMonsters(bool notifyDamage = true) {
            _nextSpawnTime = 0f;
            
            foreach (int monsterId in _aliveMonsters) {
                if (_monstersMap.TryGetValue(monsterId, out var monster)) monster.Kill(notifyDamage);
            }
            
            _armedMonsters.Clear();
            _aliveMonsters.Clear();
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: killed all monsters.");
#endif
        }

        private bool CanSpawn(ref MonsterSpawnerConfig.MonsterWave wave, ref MonsterSpawnerConfig.MonsterPreset preset, out int id) {
            id = 0;
            
            if (Time.time < _waveStartTime + preset.allowSpawnDelay ||
                _currentWaveKills < preset.allowSpawnMinKills || 
                preset.monsterIds.Length <= 0
            ) {
                return false;
            }
            
            int aliveCount = 0;
            for (int i = 0; i < preset.monsterIds.Length; i++) {
                if (_aliveMonsters.Contains(preset.monsterIds[i].value)) aliveCount++;
            }
            
            if (aliveCount >= preset.maxMonstersAtMoment) return false;

            RecreateMonsterIdsCache(preset.monsterIds);
            _monsterIdsCache.Shuffle();
            
            for (int i = 0; i < _monsterIdsCache.Length; i++) {
                id = _monsterIdsCache[i].value;
                if (_monstersMap.ContainsKey(id) && !HasSpawnExceptions(ref wave, id)) return true;
            }

            return false;
        }

        private bool HasSpawnExceptions(ref MonsterSpawnerConfig.MonsterWave wave, int monsterId) {
            for (int i = 0; i < wave.disallowSpawnTogether.Length; i++) {
                var group = wave.disallowSpawnTogether[i].theseMonsters;
                bool hasAliveMembers = false;
                bool isMember = false;

                for (int j = 0; j < group.Length; j++) {
                    int memberId = group[j].value;

                    hasAliveMembers |= _aliveMonsters.Contains(memberId);
                    isMember |= monsterId == memberId;
                }
                
                if (!hasAliveMembers && !isMember) continue;
                
                group = wave.disallowSpawnTogether[i].withTheseMonsters;
                
                for (int j = 0; j < group.Length; j++) {
                    int memberId = group[j].value;

                    if (isMember && _aliveMonsters.Contains(memberId) ||
                        hasAliveMembers && monsterId == memberId
                    ) {
                        return true;
                    }
                }
            }

            return false;
        }
        
        private void RecreateMonsterIdsCache(LabelValue[] source) {
            _monsterIdsCache ??= ArrayPool<LabelValue>.Shared.Rent(source.Length);
            
            if (_monsterIdsCache.Length != source.Length) {
                DisposeMonsterIdsCache();
                _monsterIdsCache = ArrayPool<LabelValue>.Shared.Rent(source.Length);    
            }
            
            Array.Copy(source, _monsterIdsCache, _monsterIdsCache.Length);
        }

        private void DisposeMonsterIdsCache() {
            if (_monsterIdsCache == null) return;
            
            ArrayPool<LabelValue>.Shared.Return(_monsterIdsCache);
            _monsterIdsCache = null;
        }

        private void RecreateMonsterPresetsCache(int waveIndex) {
            ref var wave = ref _config.monsterWaves[waveIndex];
            var source = wave.monsterPresets;
            
            _monsterPresetsCache ??= ArrayPool<MonsterSpawnerConfig.MonsterPreset>.Shared.Rent(source.Length);
            
            if (_monsterPresetsCache.Length != source.Length) {
                DisposeMonsterPresetsCache();
                _monsterPresetsCache = ArrayPool<MonsterSpawnerConfig.MonsterPreset>.Shared.Rent(source.Length); 
            }
            
            Array.Copy(source, _monsterPresetsCache, _monsterPresetsCache.Length);
        }

        private void DisposeMonsterPresetsCache() {
            if (_monsterPresetsCache == null) return;
            
            ArrayPool<MonsterSpawnerConfig.MonsterPreset>.Shared.Return(_monsterPresetsCache);
            _monsterPresetsCache = null;
        }
    }
    
}