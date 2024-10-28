using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Lists;
using MisterGames.Common.Pooling;
using MisterGames.Scenario.Events;
using MisterGames.Tweens;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterSpawner : MonoBehaviour {

        [SerializeField] [MinMaxSlider(0f, 100f)] private Vector2 _respawnDelay;
        [SerializeField] private EventReference _monsterKillEvent;
        [SerializeField] [Min(0)] private int _maxKills;
        [SerializeField] [Min(0)] private int _maxSpawnedMonstersAtMoment;
        [SerializeField] private AnimationCurve _respawnSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 2f);
        [SerializeField] private MonsterPreset[] _monsterPresets;
        
        [Serializable]
        private struct MonsterPreset {
            public Actor prefab;
            [Min(0)] public int minKills;
            public TweenRunner[] spawnPoints;
        }

        private struct MonsterData {
            public TweenRunner spawnPoint;
            public Monster monster;
        }
        
        private readonly Dictionary<HealthBehaviour, MonsterData> _spawnedMonstersMap = new();
        private readonly HashSet<TweenRunner> _occupiedSpawnPoints = new();
        private CancellationTokenSource _enableCts;

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            StartSpawn(_enableCts.Token).Forget();
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            ReleaseAllMonsters(destroyCancellationToken);
        }

        private void ReleaseAllMonsters(CancellationToken cancellationToken) {
            var healths = ArrayPool<HealthBehaviour>.Shared.Rent(_spawnedMonstersMap.Count);
            _spawnedMonstersMap.Keys.CopyTo(healths, 0);

            for (int i = 0; i < healths.Length; i++) {
                ReleaseMonster(healths[i], cancellationToken).Forget();
            }

            ArrayPool<HealthBehaviour>.Shared.Return(healths);
            
            _spawnedMonstersMap.Clear();
            _occupiedSpawnPoints.Clear();
        }

        private async UniTask StartSpawn(CancellationToken cancellationToken) {
            while (!cancellationToken.IsCancellationRequested && 
                   _monsterKillEvent.GetRaiseCount() < _maxKills
            ) {
                if (CanSpawnNextMonster()) TrySpawnMonster();
                
                await UniTask.Delay(TimeSpan.FromSeconds(GetRespawnDelay()), cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();
            }
        }

        private void TrySpawnMonster() {
            _monsterPresets.Shuffle();

            for (int i = 0; i < _monsterPresets.Length; i++) {
                var monsterPreset = _monsterPresets[i];
                
                if (_monsterKillEvent.GetRaiseCount() < monsterPreset.minKills ||
                    !TryGetFreeSpawnPoint(monsterPreset.spawnPoints, out var tweenRunner)
                ) {
                    continue;
                }
                
                tweenRunner.transform.GetPositionAndRotation(out var pos, out var rot);
                var actor = PrefabPool.Main.Get(monsterPreset.prefab, pos, rot);
                
                var health = actor.GetComponent<HealthBehaviour>();
                health.RestoreFullHealth();

                health.OnDeath -= OnMonsterDeath;
                health.OnDeath += OnMonsterDeath;
                
                var monster = actor.GetComponent<Monster>();
                monster.Bind(tweenRunner);

                _occupiedSpawnPoints.Add(tweenRunner);
                _spawnedMonstersMap[health] = new MonsterData { spawnPoint = tweenRunner, monster = monster };

                return;
            }
        }

        private void OnMonsterDeath(HealthBehaviour health) {
            _monsterKillEvent.Raise();
            ReleaseMonster(health, _enableCts.Token).Forget();
        }

        private async UniTask ReleaseMonster(HealthBehaviour health, CancellationToken cancellationToken) {
            health.OnDeath -= OnMonsterDeath;

            _spawnedMonstersMap.Remove(health, out var data);

            await data.monster.Unbind(cancellationToken);
            
            if (cancellationToken.IsCancellationRequested) return;
            
            PrefabPool.Main.Release(data.monster);
            _occupiedSpawnPoints.Remove(data.spawnPoint);
        }

        private bool TryGetFreeSpawnPoint(TweenRunner[] spawnPoints, out TweenRunner tweenRunner) {
            for (int i = 0; i < spawnPoints.Length; i++) {
                var spawnPoint = spawnPoints[i];
                if (_occupiedSpawnPoints.Contains(spawnPoint)) continue;

                tweenRunner = spawnPoint;
                return true;
            }

            tweenRunner = null;
            return false;
        }

        private bool CanSpawnNextMonster() {
            return _occupiedSpawnPoints.Count < _maxSpawnedMonstersAtMoment;
        }

        private float GetRespawnDelay() {
            float delay = Random.Range(_respawnDelay.x, _respawnDelay.y);
            return delay / _respawnSpeedCurve.Evaluate(_monsterKillEvent.GetRaiseCount() / (float) _maxKills);
        }
    }
    
}