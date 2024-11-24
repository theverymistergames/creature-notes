using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MisterGames.Scenario.Events;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class MonsterController : MonoBehaviour {

    [SerializeField] private float spawnTime = 40;
    [SerializeField] private float harbringerThreshold = 0.8f;
    [SerializeField] private float minDelay;
    [SerializeField] private float maxDelay;
    
    [SerializeField] private int maxMonsters = 4;
    
    [SerializeField] private Flesh[] terrains;
    
    [SerializeField] private AudioClip spawnSound;
    
    [SerializeField]
    private List<Monster> monsters;
    private bool _inProgress;

    private DebuffsController _debuffsController;

    private int _spawnedMonsters;

    private AudioSource _source;

    [SerializeField] private EventReference monsterKilledEvent;
    [SerializeField] private EventReference charDeathEvent;

    private void Start() {
        _source = GetComponent<AudioSource>();
        
        foreach (var monster in monsters) {
            monster.monsterKilled = () => OnMonsterKilled(monster);
            monster.monsterFinished = () => OnMonsterFinished(monster);
        }

        _debuffsController = FindObjectOfType<DebuffsController>();
    }

    public void Stop() {
        _inProgress = false;
        _spawnedMonsters = 0;
        
        foreach (var terrain in terrains) terrain.SetPosition(0);
        
        foreach (var monster in monsters) {
            monster.Stop();
        }
        
        StopCoroutine(SpawnRoutine());
    }

    void OnMonsterFinished(Monster monster) {
        _source.PlayOneShot(spawnSound);
        
        _spawnedMonsters++;
        UpdateFlesh();
        _debuffsController.StartDebuff(monster.type);

        if (_spawnedMonsters == maxMonsters) {
            charDeathEvent.Raise();
        }
    }

    private void OnMonsterKilled(Monster monster) {
        if (monster.IsFinished()) {
            _spawnedMonsters--;
            UpdateFlesh();
        }
        
        monsterKilledEvent.Raise();
    }

    void UpdateFlesh() {
        foreach (var terrain in terrains) terrain.SetPosition(_spawnedMonsters);
    }

    public void SpawnSingle(MonsterType type) {
        var m = monsters.Find(m => m.type == type);
        
        if (m) {
            SpawnMonster(m);
        }
    }

    private void SpawnMonster(Monster monster) {
        Debug.Log(monster.type);
        monster.Spawn(spawnTime, harbringerThreshold);
    }

    private IEnumerator SpawnRoutine() {
        if (!_inProgress) yield break;
        
        yield return new WaitForSeconds(minDelay + (maxDelay - minDelay) * Random.Range(0, 1f));
        
        if (!_inProgress) yield break;

        if (monsters.Find(m => !m.IsSpawned() && m.IsEnabled()) && monsters.FindAll(m => m.IsSpawned()).Count < maxMonsters) {
            var index = Random.Range(0, monsters.Count);
            while (monsters[index].IsSpawned() || !monsters[index].IsEnabled()) index = Random.Range(0, monsters.Count);
            SpawnMonster(monsters[index]);
        }

        StartCoroutine(SpawnRoutine());
    }

    public void StartSpawn() {
        if (_inProgress) return;
        _inProgress = true;

        StartCoroutine(SpawnRoutine());
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.M)) {
            StartSpawn();
        }
    }
}
