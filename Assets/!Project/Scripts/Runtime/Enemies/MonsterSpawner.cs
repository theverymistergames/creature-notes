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
using MisterGames.Tick.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterSpawner : MonoBehaviour, IUpdate {

        [SerializeField] private MonsterSpawnerConfig _config;
        [SerializeField] private FleshController[] _fleshControllers;
        [SerializeField] private Monster[] _monsters;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo;
        
        private readonly Dictionary<int, Monster> _monstersMap = new();
        private readonly Dictionary<int, float> _monsterKillTimeMap = new();
        private readonly HashSet<int> _aliveMonsters = new();
        private readonly HashSet<int> _armedMonsters = new();
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

        private void Awake() {
            FetchMonstersMap();
        }

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

        private void FetchMonstersMap() {
            for (int i = 0; i < _monsters.Length; i++) {
                var monster = _monsters[i];
                _monstersMap.Add(monster.Id, monster);
            }
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
            
            for (int i = 0; i < wave.monsterPresets.Length; i++) {
                ref var preset = ref wave.monsterPresets[i];

                for (int j = 0; j < preset.monsterIds.Length; j++) {
                    int monsterId = preset.monsterIds[j].GetValue();
                    
                    if (!_aliveMonsters.Contains(monsterId) || !_monstersMap.TryGetValue(monsterId, out var monster)) {
                        continue;
                    }
                    
                    if (monster.IsDead) {
                        _aliveMonsters.Remove(monsterId);
                        _armedMonsters.Remove(monsterId);
                        
                        _monsterKillTimeMap[monsterId] = time;
                        
                        _currentWaveKills++;
                        _totalKills++;
                        _config.monsterKilledEvent.Raise();
                    
#if UNITY_EDITOR
                        if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, killed monster [{preset.monsterIds[j]}]. " +
                                                      $"Kills per wave {_currentWaveKills}/{wave.killsToCompleteWave}, " +
                                                      $"kills total {_totalKills}, " +
                                                      $"alive monsters {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}.");
#endif
                    
                        SetTargetFleshProgress(GetTargetFleshProgress(waveIndex));
                        continue;
                    }

                    if (monster.IsArmed && _armedMonsters.Add(monsterId)) {
#if UNITY_EDITOR
                        if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, armed monster [{preset.monsterIds[j]}]. " +
                                                      $"Alive monsters {_aliveMonsters.Count}/{wave.maxAliveMonstersAtMoment}, " +
                                                      $"armed monsters {_armedMonsters.Count}/{wave.armedMonstersToKillCharacter}.");
#endif
                        
                        SetTargetFleshProgress(GetTargetFleshProgress(waveIndex));
                        continue;
                    }
                
                    if (!monster.IsArmed && _armedMonsters.Contains(monsterId)) {
                        _armedMonsters.Remove(monsterId);
                        SetTargetFleshProgress(GetTargetFleshProgress(waveIndex));
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

            int length = wave.monsterPresets.Length;
            RecreateAndShuffleArray(ref _indicesCache, length);
            
            for (int i = 0; i < length; i++) {
                ref var preset = ref wave.monsterPresets[_indicesCache[i]];
                
                if (!CanSpawnMonsterFromPreset(ref wave, ref preset, out int newMonsterId) || 
                    !_monstersMap.TryGetValue(newMonsterId, out var monster)) 
                {
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
                    preset.monsterIds.TryFind(newMonsterId, (l, id) => l.GetValue() == id, out var labelValue);
                    Debug.Log($"MonsterSpawner [{name}]: wave {waveIndex}, respawned monster [{labelValue.GetLabel()}] with arm duration {armDuration} s. " +
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
            _fleshProgressSmoothed = _fleshProgressSmoothed.SmoothExpNonZero(_fleshProgressTarget, dt * _config.fleshProgressSmoothing);
            
            ApplyFleshProgress(_fleshProgressSmoothed);      
        }
        
        private void ApplyFleshProgress(float progress) {
            for (int i = 0; i < _fleshControllers.Length; i++) {
                _fleshControllers[i].ApplyProgress(progress);
            }
        }

        private void KillAllMonsters(bool notifyDamage = true) {
            _nextSpawnTime = 0f;
            
            foreach (int monsterId in _aliveMonsters) {
                if (_monstersMap.TryGetValue(monsterId, out var monster)) monster.Kill(notifyDamage);
            }
            
            _armedMonsters.Clear();
            _aliveMonsters.Clear();
            _monsterKillTimeMap.Clear();
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: killed all monsters.");
#endif
            
            ApplyFleshProgress(0f);
        }

        private bool CanSpawnMonsterFromPreset(
            ref MonsterSpawnerConfig.MonsterWave wave,
            ref MonsterSpawnerConfig.MonsterPreset preset, 
            out int id) 
        {
            id = 0;
            
            if (Time.time < _waveStartTime + preset.allowSpawnDelayAfterWaveStart ||
                _currentWaveKills < preset.allowSpawnMinKills || 
                preset.monsterIds.Length <= 0
            ) {
                return false;
            }
            
            int aliveCount = 0;
            for (int i = 0; i < preset.monsterIds.Length; i++) {
                if (_aliveMonsters.Contains(preset.monsterIds[i].GetValue())) aliveCount++;
            }
            
            if (aliveCount >= preset.maxMonstersAtMoment) return false;

            int length = preset.monsterIds.Length;
            RecreateAndShuffleArray(ref _indicesCache, length);
            
            for (int i = 0; i < length; i++) {
                id = preset.monsterIds[_indicesCache[i]].GetValue();
                if (_monstersMap.ContainsKey(id) && CanSpawnMonster(ref wave, ref preset, id)) return true;
            }

            return false;
        }

        private bool CanSpawnMonster(
            ref MonsterSpawnerConfig.MonsterWave wave,
            ref MonsterSpawnerConfig.MonsterPreset preset,
            int monsterId) 
        {
            if (_monsterKillTimeMap.TryGetValue(monsterId, out float killTime) && 
                Time.time < killTime + preset.respawnCooldownAfterKill) 
            {
                return false;
            }
            
            for (int i = 0; i < wave.disallowSpawnTogether.Length; i++) {
                var group = wave.disallowSpawnTogether[i].theseMonsters;
                bool hasAliveMembers = false;
                bool isMember = false;

                for (int j = 0; j < group.Length; j++) {
                    int memberId = group[j].GetValue();

                    hasAliveMembers |= _aliveMonsters.Contains(memberId);
                    isMember |= monsterId == memberId;
                }
                
                if (!hasAliveMembers && !isMember) continue;
                
                group = wave.disallowSpawnTogether[i].withTheseMonsters;
                
                for (int j = 0; j < group.Length; j++) {
                    int memberId = group[j].GetValue();

                    if (isMember && _aliveMonsters.Contains(memberId) ||
                        hasAliveMembers && monsterId == memberId
                    ) {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void RecreateAndShuffleArray<T>(ref T[] dest, int length) {
            dest ??= ArrayPool<T>.Shared.Rent(length);
            
            if (dest.Length < length) {
                ArrayPool<T>.Shared.Return(dest);
                dest = ArrayPool<T>.Shared.Rent(length);
            }
            
            dest.Shuffle(length);
        }

        private static void DisposeArray<T>(ref T[] indices) {
            if (indices != null) ArrayPool<T>.Shared.Return(indices);
        }
    }
    
}