using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Lists;
using MisterGames.Scenario.Events;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterSpawner : MonoBehaviour {

        [SerializeField] private EventReference _spawnedMonsterEvent;
        [SerializeField] private EventReference _killMonsterTotalEvent;
        [SerializeField] private EventReference _killMonsterPerWaveEvent;
        [SerializeField] private EventReference _startedWavesEvent;
        [SerializeField] private EventReference _completedWavesEvent;
        [SerializeField] private MonsterWave[] _monsterWaves;
        
        [Serializable]
        private struct MonsterWave {
            [Min(0f)] public float startDelay;
            [MinMaxSlider(0f, 100f)] public Vector2 respawnDelayRangeStart;
            [MinMaxSlider(0f, 100f)] public Vector2 respawnDelayRangeEnd;
            [Min(-1)] public int killsToCompleteWave;
            [Min(-1)] public int maxSpawnedMonstersAtMoment;
            public MonsterPreset[] monsterPresets;
        }
        
        [Serializable]
        private struct MonsterPreset {
            public Monster monster;
            [Min(0f)] public float allowSpawnDelay;
            [Min(0f)] public int minKillsToAllowSpawn;
        }

        private readonly HashSet<Monster> _aliveMonsters = new();
        private CancellationTokenSource _enableCts;
        private byte _spawnProcessId;
        private float _waveStartTime;
        private float _nextSpawnTime;
        private int _currentWave = -1;

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            StartSpawning();
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            StopSpawning();
        }

        private void StartSpawning() {
            if (!enabled) return;
            
            KillAllMonsters(instant: true);
            StartSpawningAsync(_enableCts.Token).Forget();
        }

        private void StopSpawning() {
            _spawnProcessId++;
            KillAllMonsters(instant: false);
        }

        private async UniTask StartSpawningAsync(CancellationToken cancellationToken) {
            byte id = ++_spawnProcessId;

            while (id == _spawnProcessId && !cancellationToken.IsCancellationRequested) {
                if (TryFinishWave(ref _currentWave)) {
                    KillAllMonsters(instant: false);
                    
                    if (_currentWave >= _monsterWaves.Length) break;

                    var wave = _monsterWaves[_currentWave];
                    await UniTask.Delay(TimeSpan.FromSeconds(wave.startDelay), cancellationToken: cancellationToken)
                        .SuppressCancellationThrow();
         
                    if (id != _spawnProcessId || cancellationToken.IsCancellationRequested) break;
                    
                    _startedWavesEvent.SetCount(_currentWave + 1);
                    _waveStartTime = Time.time;
                }
                
                CheckKills(_currentWave);
                CheckSpawns(_currentWave, _waveStartTime);

                await UniTask.Yield();
            }
        }
        
        private bool TryFinishWave(ref int waveIndex) {
            int completedWaves = _completedWavesEvent.GetRaiseCount();
            
            if (waveIndex < 0 || waveIndex >= _monsterWaves.Length) {
                waveIndex = completedWaves;
                return true;
            }

            var wave = _monsterWaves[waveIndex];
            
            int kills = _killMonsterPerWaveEvent.WithSubId(waveIndex).GetRaiseCount();
            if (kills < wave.killsToCompleteWave) return false;
            
            _completedWavesEvent.SetCount(++waveIndex);
            return true;
        }

        private void CheckKills(int waveIndex) {
            ref var wave = ref _monsterWaves[waveIndex];
            
            for (int i = 0; i < wave.monsterPresets.Length; i++) {
                ref var preset = ref wave.monsterPresets[i];
                if (!preset.monster.IsDead || !_aliveMonsters.Contains(preset.monster)) continue;

                _aliveMonsters.Remove(preset.monster);
                _killMonsterPerWaveEvent.WithSubId(waveIndex).Raise();
                _killMonsterTotalEvent.Raise();
            }
        }

        private void CheckSpawns(int waveIndex, float waveStartTime) {
            ref var wave = ref _monsterWaves[waveIndex];
            float time = Time.time;
            
            if (_aliveMonsters.Count >= wave.maxSpawnedMonstersAtMoment || time < _nextSpawnTime) return;
            
            int kills = _killMonsterPerWaveEvent.WithSubId(waveIndex).GetRaiseCount();
            wave.monsterPresets.Shuffle();

            for (int i = 0; i < wave.monsterPresets.Length; i++) {
                ref var preset = ref wave.monsterPresets[i];
                
                if (!preset.monster.IsDead ||
                    time < waveStartTime + preset.allowSpawnDelay ||
                    kills < preset.minKillsToAllowSpawn
                ) {
                    continue;
                }
                
                preset.monster.Respawn();
                _aliveMonsters.Add(preset.monster);
                _spawnedMonsterEvent.Raise();
                
                float t = wave.killsToCompleteWave > 0 ? (float) kills / wave.killsToCompleteWave : 1f;
                float respawnDelay = Mathf.Lerp(
                    Random.Range(wave.respawnDelayRangeStart.x, wave.respawnDelayRangeStart.y),
                    Random.Range(wave.respawnDelayRangeEnd.x, wave.respawnDelayRangeEnd.y),
                    t
                );
                
                _nextSpawnTime = time + respawnDelay;
                
                return;
            }
        }

        private void KillAllMonsters(bool instant = false) {
            _nextSpawnTime = 0f;
            
            foreach (var monster in _aliveMonsters) {
                monster.Kill(instant);
            }
            
            _aliveMonsters.Clear();
        }
    }
    
}