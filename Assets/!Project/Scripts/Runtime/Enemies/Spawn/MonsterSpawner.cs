using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using _Project.Scripts.Runtime.Flesh;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Labels;
using MisterGames.Common.Lists;
using MisterGames.Common.Maths;
using MisterGames.Common.Tick;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterSpawner : MonoBehaviour, IUpdate {

        [Header("Configs")]
        [SerializeField] private MonsterSpawnerConfig _config;
        [SerializeField] private Config[] _configsByLevels;
        [SerializeField] private EventReference _currentLevelEvent;
        
        [Header("Flesh")]
        [SerializeField] private FleshControllerGroup _fleshControllerGroup;
        [SerializeField] [Min(0f)] private float _fleshProgressSmoothing = 1f;

        [Header("Monsters")]
        [SerializeField] private Transform _monstersRoot;

        [Serializable]
        private struct Config {
            [Min(0)] public int level;
            public MonsterSpawnerConfig config;
        }
        
        public bool IsBattleRunning { get; private set; }
        
        private readonly Dictionary<int, int> _aliveMonsterTypeMap = new();
        private readonly HashSet<Monster> _aliveMonsters = new();
        private readonly HashSet<Monster> _armedMonsters = new();
        private Monster[] _monsters;
        private float[] _monstersKillTimers;
        private int[] _presetIndicesCache;
        private int[] _monsterIndicesCache;

        private CancellationTokenSource _enableCts;
        private byte _spawnProcessId;
        private float _waveTimer;
        private float _nextSpawnTimer;
        private float _nextSpawnDelay;
        
        private int _currentWave;
        private int _currentWaveKills;
        private int _totalKills;
        private bool _characterKilled;
        
        private float _fleshProgressTarget;
        private float _fleshProgressSmoothed;
        private bool _forceUpdateFlesh;

        private void Awake() {
            _monsters = _monstersRoot.GetComponentsInChildren<Monster>();
            _monstersKillTimers = new float[_monsters.Length];
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            PlayerLoopStage.Update.Subscribe(this);
            
            _forceUpdateFlesh = true;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            PlayerLoopStage.Update.Unsubscribe(this);
            
            StopSpawning(instantReset: true);
        }

        void IUpdate.OnUpdate(float dt) {
            UpdateTimers(dt);
            UpdateFleshProgress(dt);
        }

        public bool TryGetCurrentConfig(out MonsterSpawnerConfig config) {
            config = _config;
            return config != null;
        }
        
        public bool TryGetConfigForCurrentLevel(out MonsterSpawnerConfig config) {
            int level = _currentLevelEvent.GetCount();
            
            for (int i = 0; i < _configsByLevels.Length; i++) {
                ref var c = ref _configsByLevels[i];
                if (c.level != level) continue;

                config = c.config;
                return true;
            }

            config = null;
            return false;
        }
        
        public void StartSpawning(MonsterSpawnerConfig config, bool instantReset) {
            if (config == null) {
                Debug.LogWarning($"MonsterSpawner [{name}]: trying to start spawning with null config, skip.");
                return;
            }

            _config = config;
            
            _currentWave = -1;
            _currentWaveKills = 0;
            _totalKills = 0;
            _characterKilled = false;
            
            _waveTimer = 0f;
            _nextSpawnDelay = 0f;
            _nextSpawnTimer = 0f;
            
            IsBattleRunning = true;
            _forceUpdateFlesh = true;
            
            for (int i = 0; i < _monstersKillTimers.Length; i++) {
                _monstersKillTimers[i] = 0f;
            }

#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: start spawning, " +
                                          $"waves completed {GetCompletedWaves()}/{_config.monsterWaves.Length}.");
#endif
            
            KillAllMonsters(instantReset);
            StartSpawningAsync(_enableCts.Token).Forget();
        }

        public void ContinueSpawningFromCompletedWaves(bool instantReset) {
            if (_config == null) {
                Debug.LogWarning($"MonsterSpawner [{name}]: trying to continue spawning with null config, skip.");
                return;
            }

            int completedWaves = GetCompletedWaves();

            // Setup previous completed wave to start from next wave.
            _currentWave = completedWaves - 1;
            _currentWaveKills = _currentWave >= 0 && _currentWave < _config.monsterWaves.Length
                ? _config.monsterWaves[_currentWave].killsToCompleteWave
                : 0;
            
            _totalKills = GetTotalKills(completedWaves);
            _characterKilled = false;
            
            _waveTimer = 0f;
            _nextSpawnDelay = 0f;
            _nextSpawnTimer = 0f;
            
            IsBattleRunning = true;
            _forceUpdateFlesh = true;
            
            for (int i = 0; i < _monstersKillTimers.Length; i++) {
                _monstersKillTimers[i] = 0f;
            }
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: continue spawning, " +
                                          $"waves completed {completedWaves}/{_config.monsterWaves.Length}.");
#endif
            
            KillAllMonsters(instantReset);
            StartSpawningAsync(_enableCts.Token).Forget();
        }
        
        public void StopSpawning(bool instantReset) {
            _spawnProcessId++;
            
            KillAllMonsters(instantReset);
            
            DisposeArray(ref _presetIndicesCache);
            DisposeArray(ref _monsterIndicesCache);
            
            _forceUpdateFlesh = true;
            IsBattleRunning = false;
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: stopped spawning, " +
                                          $"waves completed {GetCompletedWaves()}/{(_config == null ? 0 : _config.monsterWaves.Length)}.");
#endif
        }

        private void UpdateTimers(float dt) {
            _waveTimer += dt;
            _nextSpawnTimer += dt;
            
            for (int i = 0; i < _monstersKillTimers.Length; i++) {
                ref float timer = ref _monstersKillTimers[i];
                timer += dt;
            }
        }

        private int GetCompletedWaves() {
            return Mathf.Max(0, _currentWave - 1);
        }

        private int GetTotalKills(int completedWaves) {
            int kills = 0;
            
            for (int i = 0; i <= completedWaves; i++) {
                ref var wave = ref _config.monsterWaves[i];
                kills += wave.killsToCompleteWave;
            }
            
            return kills;
        }
        
        private async UniTask StartSpawningAsync(CancellationToken cancellationToken) {
            byte id = ++_spawnProcessId;

            while (id == _spawnProcessId && !cancellationToken.IsCancellationRequested) {
                if (TryFinishWave(ref _currentWave)) {
                    if (_currentWave >= _config.monsterWaves.Length) {
                        CompleteBattle();
                        break;
                    }
                    
                    KillAllMonsters(instantReset: false);
                    
                    var wave = _config.monsterWaves[_currentWave];
                    
#if UNITY_EDITOR
                    if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: starting wave {_currentWave} in {wave.startDelay} s.");
#endif
                    
                    await UniTask.Delay(TimeSpan.FromSeconds(wave.startDelay), cancellationToken: cancellationToken)
                        .SuppressCancellationThrow();
         
                    if (id != _spawnProcessId || cancellationToken.IsCancellationRequested) break;

#if UNITY_EDITOR
                    if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: started wave {_currentWave}, " +
                                                  $"kills to complete wave {wave.killsToCompleteWave}, " +
                                                  $"max alive monsters {wave.maxAliveMonstersAtMoment}, " +
                                                  $"armed monsters to kill character {wave.armedMonstersToKillCharacter}");
#endif

                    _currentWaveKills = 0;
                    _waveTimer = 0f;
                    _nextSpawnDelay = 0f;
                    _nextSpawnTimer = 0f;
                    
                    _config.startedWaveEvent.Raise<int>(_currentWave);
                }

                if (CheckCanKillCharacter()) {
                    break;
                }
                
                CheckAliveMonsters(_currentWave);
                CheckSpawns(_currentWave);

                await UniTask.Yield();
            }
        }
        
        private bool TryFinishWave(ref int waveIndex) {
            if (waveIndex < 0) {
                waveIndex = 0;
                
#if UNITY_EDITOR
                if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: started first wave, wave index {waveIndex}.");
#endif
                return true;
            }
            
            if (waveIndex >= _config.monsterWaves.Length) {
                waveIndex = _config.monsterWaves.Length;
                
#if UNITY_EDITOR
                if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: all waves completed, set wave index {waveIndex}.");
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

            _config.completedWaveEvent.Raise<int>(waveIndex);
            waveIndex++;

            return true;
        }

        private void CompleteBattle() {
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: completed battle, " +
                                          $"waves {_currentWave}/{_config.monsterWaves.Length}, " +
                                          $"kills total {_totalKills}.");
#endif
            
            if (_config.killAllMonstersOnCompleteBattle) {
                KillAllMonsters(instantReset: false);
            }

            IsBattleRunning = false;
            _config.completedBattleEvent.Raise();
        }

        private void CheckAliveMonsters(int waveIndex) {
            ref var wave = ref _config.monsterWaves[waveIndex];

            for (int i = 0; i < _monsters.Length; i++) {
                var monster = _monsters[i];
                int monsterType = monster.TypeId;
                
                if (monster.IsDead && _aliveMonsters.Remove(monster)) {
                    _armedMonsters.Remove(monster);
                    
                    int monsterTypeAliveCount = _aliveMonsterTypeMap.GetValueOrDefault(monsterType) - 1;
                    if (monsterTypeAliveCount > 0) _aliveMonsterTypeMap[monsterType] = monsterTypeAliveCount;
                    else _aliveMonsterTypeMap.Remove(monsterType);
                        
                    _monstersKillTimers[i] = 0f;
                    
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
                
                if (!monster.IsArmed && _armedMonsters.Remove(monster)) {
                    SetTargetFleshProgress(GetTargetFleshProgress(waveIndex));
                }
            }
        }

        private void CheckSpawns(int waveIndex) {
            ref var wave = ref _config.monsterWaves[waveIndex];

            if (_nextSpawnTimer < _nextSpawnDelay ||
                wave.maxAliveMonstersAtMoment >= 0 && _aliveMonsters.Count >= wave.maxAliveMonstersAtMoment
            ) {
                return;
            }

            int length = wave.monsterPresets.Length;
            RecreateAndShuffleIndices(ref _presetIndicesCache, length);

            for (int i = 0; i < length; i++) {
                ref var preset = ref wave.monsterPresets[_presetIndicesCache[i]];
                
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
                
                _nextSpawnTimer = 0f;
                _nextSpawnDelay = respawnDelay;
                
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

        private bool CheckCanKillCharacter() {
            if (_characterKilled || _fleshProgressSmoothed < _config.killCharacterAtFleshProgress) return false;
                
#if UNITY_EDITOR
            int killsToCompleteWave = _currentWave >= 0 && _currentWave < _config.monsterWaves.Length
                ? _config.monsterWaves[_currentWave].killsToCompleteWave
                : 0;
            
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: flesh reached critical level {_config.killCharacterAtFleshProgress}, killing hero. " +
                                          $"waves {_currentWave}/{_config.monsterWaves.Length}, " +
                                          $"kills per wave {_currentWaveKills}/{killsToCompleteWave}, " +
                                          $"kills total {_totalKills}.");
#endif

            _characterKilled = true;
            _config.killCharacterEvent.Raise();
            IsBattleRunning = false;

            return true;
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

        private void UpdateFleshProgress(float dt) {
            float oldProgress = _fleshProgressSmoothed;
            _fleshProgressSmoothed = _fleshProgressSmoothed.SmoothExpNonZero(_fleshProgressTarget, _fleshProgressSmoothing, dt);
            
            if (oldProgress.IsNearlyEqual(_fleshProgressSmoothed) && !_forceUpdateFlesh) return;

            _forceUpdateFlesh = false;
            _fleshControllerGroup.ApplyProgress(_fleshProgressSmoothed);      
        }
        
        private void KillAllMonsters(bool instantReset) {
            _nextSpawnTimer = 0f;
            _nextSpawnDelay = float.MaxValue;
            
            foreach (var monster in _aliveMonsters) {
                monster.Kill(instantReset);
            }
            
            _armedMonsters.Clear();
            _aliveMonsters.Clear();
            _aliveMonsterTypeMap.Clear();
            
#if UNITY_EDITOR
            if (_showDebugInfo) Debug.Log($"MonsterSpawner [{name}]: killed all monsters.");
#endif

            SetTargetFleshProgress(0f);
            _forceUpdateFlesh = true;
            
            if (instantReset) _fleshProgressSmoothed = 0f;
        }

        private bool CanSpawnMonsterFromPreset(
            ref MonsterSpawnerConfig.MonsterWave wave,
            ref MonsterSpawnerConfig.MonsterPreset preset, 
            out Monster monster) 
        {
            monster = null;

            if (_waveTimer < preset.allowSpawnDelayAfterWaveStart ||
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
            
            int length = _monsters.Length;
            RecreateAndShuffleIndices(ref _monsterIndicesCache, length);
            
            for (int i = 0; i < length; i++) {
                int idx = _monsterIndicesCache[i];
                var m = _monsters[idx];
                
                if (m.TypeId != monsterType ||
                    !m.IsDead || _monstersKillTimers[idx] < preset.respawnCooldownAfterKill) 
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

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo;

        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void StartSpawnCurrentConfig() {
            StartSpawning(_config, instantReset: true);
        }
        
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void ContinueCompletedWaves() {
            ContinueSpawningFromCompletedWaves(instantReset: true);
        }
        
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void StopSpawn() {
            StopSpawning(instantReset: true);
        }
#endif
    }
    
}