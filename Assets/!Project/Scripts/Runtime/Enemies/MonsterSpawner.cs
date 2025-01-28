using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using _Project.Scripts.Runtime.Flesh;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Async;
using MisterGames.Common.Labels;
using MisterGames.Common.Lists;
using MisterGames.Common.Maths;
using MisterGames.Scenario.Events;
using MisterGames.Common.Tick;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterSpawner : MonoBehaviour, IUpdate {

        [SerializeField] private MonsterSpawnerConfig _config;
        [SerializeField] private FleshController[] _fleshControllers;
        [SerializeField] private Monster[] _monsters;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo;
        
        private readonly Dictionary<Monster, float> _monsterKillTimeMap = new();
        private readonly Dictionary<int, int> _aliveMonsterTypeMap = new();
        private readonly HashSet<Monster> _aliveMonsters = new();
        private readonly HashSet<Monster> _armedMonsters = new();
        private readonly List<Monster> _monstersBuffer = new();
        private int[] _indicesCache;

        private CancellationTokenSource _enableCts;
        private byte _spawnProcessId;
        private float _waveStartTime;
        private float _nextSpawnTime;
        
        private int _currentWave;
        private int _currentWaveKills;
        private int _totalKills;

        private bool _characterKilled;
        private float _fleshProgressTarget;
        private float _fleshProgressSmoothed;

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            PlayerLoopStage.Update.Subscribe(this);
            StartSpawning();
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            PlayerLoopStage.Update.Unsubscribe(this);
            StopSpawning();
        }

        void IUpdate.OnUpdate(float dt) {
            UpdateFleshProgressSmoothed(dt);
        }

        private void StartSpawning() {
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: start spawning, " +
                                          $"waves completed {_config.completedWavesCounter.GetCount()}/{_config.monsterWaves.Length}.");
#endif

            _currentWave = -1;
            _totalKills = 0;
            _currentWaveKills = 0;
            _characterKilled = false;
            
            KillAllMonsters(notifyDamage: false);
            StartSpawningAsync(_enableCts.Token).Forget();
        }

        private void StopSpawning() {
            _spawnProcessId++;
            
            KillAllMonsters(notifyDamage: false);
            DisposeArray(ref _indicesCache);
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
                    if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: started wave {_currentWave}. " +
                                                  $"kills to complete wave {wave.killsToCompleteWave}, " +
                                                  $"max alive monsters {wave.maxAliveMonstersAtMoment}, " +
                                                  $"armed monsters to kill character {wave.armedMonstersToKillCharacter}");
#endif
                    
                    _config.startedWaveEvent.Raise();
                    _waveStartTime = Time.time;
                }
                
                CheckCanKillCharacter();
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
            float time = Time.time;

            for (int i = 0; i < _monsters.Length; i++) {
                var monster = _monsters[i];
                int monsterType = monster.TypeId;
                
                if (monster.IsDead) {
                    _aliveMonsters.Remove(monster);
                    _armedMonsters.Remove(monster);
                    
                    int monsterTypeAliveCount = _aliveMonsterTypeMap.GetValueOrDefault(monsterType) - 1;
                    if (monsterTypeAliveCount > 0) _aliveMonsterTypeMap[monsterType] = monsterTypeAliveCount;
                    else _aliveMonsterTypeMap.Remove(monsterType);
                        
                    _monsterKillTimeMap[monster] = time;
                        
                    _currentWaveKills++;
                    _totalKills++;
                    _config.monsterKilledEvent.Raise();
                    
#if UNITY_EDITOR
                    if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, killed monster [{monster}]. " +
                                                  $"Kills per wave {_currentWaveKills}/{wave.killsToCompleteWave}, " +
                                                  $"kills total {_totalKills}, " +
                                                  $"alive monsters {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}.");
#endif
                    
                    SetTargetFleshProgress(GetTargetFleshProgress(waveIndex));
                    continue;
                }

                if (monster.IsArmed && _armedMonsters.Add(monster)) {
#if UNITY_EDITOR
                    if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, armed monster [{monster}]. " +
                                                  $"Alive monsters {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}, " +
                                                  $"armed monsters {_armedMonsters.Count}/{wave.armedMonstersToKillCharacter}.");
#endif
                        
                    SetTargetFleshProgress(GetTargetFleshProgress(waveIndex));
                    continue;
                }
                
                if (!monster.IsArmed && _armedMonsters.Contains(monster)) {
                    _armedMonsters.Remove(monster);
                    SetTargetFleshProgress(GetTargetFleshProgress(waveIndex));
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

            int length = wave.monsterPresets.Length;
            RecreateAndShuffleIndices(ref _indicesCache, length);
            
            for (int i = 0; i < length; i++) {
                ref var preset = ref wave.monsterPresets[_indicesCache[i]];
                if (!CanSpawnMonsterFromPreset(ref wave, ref preset, out var newMonster)) continue;
                
                int monsterType = newMonster.TypeId;
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
                
                var attackDurationRange = new Vector2(
                    Mathf.Lerp(preset.attackDurationStart.x, preset.attackDurationEnd.x, t), 
                    Mathf.Lerp(preset.attackDurationStart.y, preset.attackDurationEnd.y, t)
                );

                var attackCooldownRange = new Vector2(
                    Mathf.Lerp(preset.attackCooldownStart.x, preset.attackCooldownEnd.x, t), 
                    Mathf.Lerp(preset.attackCooldownStart.y, preset.attackCooldownEnd.y, t)
                );
                
                newMonster.Respawn(armDuration, attackDurationRange, attackCooldownRange);
                
                _aliveMonsters.Add(newMonster);
                _aliveMonsterTypeMap[monsterType] = _aliveMonsterTypeMap.GetValueOrDefault(monsterType) + 1;
                _nextSpawnTime = time + respawnDelay;
                
#if UNITY_EDITOR
                if (_showDebugInfo) {
                    Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, respawned monster [{newMonster}] with arm duration {armDuration} s. " +
                              $"Next respawn available in {respawnDelay} s. " +
                              $"Alive monsters {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}.");
                    
                }
#endif
                
                return;
            }
        }

        private void CheckCanKillCharacter() {
            if (_characterKilled || _fleshProgressSmoothed < _config.killCharacterAtFleshProgress) return;

            _config.killCharacterEvent.Raise();
        }

        private float GetTargetFleshProgress(int waveIndex) {
            ref var wave = ref _config.monsterWaves[waveIndex];

            return wave.armedMonstersToKillCharacter > 0f
                ? Mathf.Clamp01((float) _armedMonsters.Count / wave.armedMonstersToKillCharacter)
                : 1f;
        }
        
        private void SetTargetFleshProgress(float progress) {
            _fleshProgressTarget = progress;
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: update flesh progress to {_fleshProgressTarget:0.00}.");
#endif
        }

        private void UpdateFleshProgressSmoothed(float dt) {
            _fleshProgressSmoothed = _fleshProgressSmoothed.SmoothExpNonZero(_fleshProgressTarget, _config.fleshProgressSmoothing, dt);
            
            ApplyFleshProgress(_fleshProgressSmoothed);      
        }
        
        private void ApplyFleshProgress(float progress) {
            for (int i = 0; i < _fleshControllers.Length; i++) {
                _fleshControllers[i].ApplyProgress(progress);
            }
        }

        private void KillAllMonsters(bool notifyDamage = true) {
            _nextSpawnTime = 0f;
            
            foreach (var monster in _aliveMonsters) {
                monster.Kill(notifyDamage);
            }
            
            _armedMonsters.Clear();
            _aliveMonsters.Clear();
            _aliveMonsterTypeMap.Clear();
            _monsterKillTimeMap.Clear();
            _monstersBuffer.Clear();
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: killed all monsters.");
#endif
            
            ApplyFleshProgress(0f);
        }

        private bool CanSpawnMonsterFromPreset(
            ref MonsterSpawnerConfig.MonsterWave wave,
            ref MonsterSpawnerConfig.MonsterPreset preset, 
            out Monster monster) 
        {
            monster = null;
            
            if (Time.time < _waveStartTime + preset.allowSpawnDelayAfterWaveStart ||
                _currentWaveKills < preset.allowSpawnMinKills
            ) {
                return false;
            }
            
            int monsterType = preset.monsterType.GetValue();
            int aliveCount = _aliveMonsterTypeMap.GetValueOrDefault(monsterType);

            if (aliveCount >= preset.maxMonstersAtMoment ||
                !CanSpawnMonsterOfType(monsterType, wave.disallowSpawnTogether)) 
            {
                return false;
            }
            
            _monstersBuffer.Clear();

            for (int i = 0; i < _monsters.Length; i++) {
                var m = _monsters[i];
                if (m.IsDead) _monstersBuffer.Add(m);
            }

            _monstersBuffer.Shuffle();
            
            for (int i = 0; i < _monstersBuffer.Count; i++) {
                var m = _monstersBuffer[i];
                
                if (_monsterKillTimeMap.TryGetValue(m, out float killTime) && 
                    Time.time < killTime + preset.respawnCooldownAfterKill) 
                {
                    continue;
                }
                
                monster = m;
                return true;
            }

            return false;
        }

        private bool CanSpawnMonsterOfType(int monsterType, MonsterSpawnerConfig.SpawnExceptionGroup[] disallowSpawnTogether) {
            for (int i = 0; i < disallowSpawnTogether.Length; i++) {
                var group = disallowSpawnTogether[i].theseMonsters;
                bool hasAliveMembers = false;
                bool isMember = false;

                for (int j = 0; j < group.Length; j++) { 
                    int memberType = group[j].GetValue();

                    hasAliveMembers |= _aliveMonsterTypeMap.GetValueOrDefault(memberType) > 0;
                    isMember |= monsterType == memberType;
                }
                
                if (!hasAliveMembers && !isMember) continue;
                
                group = disallowSpawnTogether[i].withTheseMonsters;
                
                for (int j = 0; j < group.Length; j++) {
                    int memberType = group[j].GetValue();

                    if (isMember && _aliveMonsterTypeMap.GetValueOrDefault(memberType) > 0 ||
                        hasAliveMembers && monsterType == memberType) 
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void RecreateAndShuffleIndices(ref int[] dest, int length) {
            dest ??= ArrayPool<int>.Shared.Rent(length);
            
            if (dest.Length < length) {
                ArrayPool<int>.Shared.Return(dest);
                dest = ArrayPool<int>.Shared.Rent(length);
            }

            for (int i = 0; i < length; i++) {
                dest[i] = i;
            }

            dest.Shuffle(length);
        }

        private static void DisposeArray<T>(ref T[] array) {
            if (array != null) ArrayPool<T>.Shared.Return(array);
        }
    }
    
}