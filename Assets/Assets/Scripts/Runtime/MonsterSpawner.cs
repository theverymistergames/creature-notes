using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MisterGames.Scenario.Events;
using Unity.VisualScripting;
using UnityEngine;
using Random = System.Random;

public class MonsterSpawner : MonoBehaviour {

    [SerializeField] private float minDelay;
    [SerializeField] private float maxDelay;
    
    [SerializeField] private int maxMonsters = 4;
    
    [SerializeField]
    private List<Monster> monsters;
    private bool _inProgress;
    private Random _random;

    private DebuffsController _debuffsController;

    [SerializeField] private EventReference monsterKilledEvent;

    private void Start() {
        _random = new Random();
        
        foreach (var monster in monsters) {
            monster.monsterKilled = () => OnMonsterKilled(monster);
            monster.monsterFinished = () => OnMonsterFinished(monster);
        }

        _debuffsController = FindObjectOfType<DebuffsController>();
    }

    public void Stop()
    {
        _inProgress = false;
        
        foreach (var monster in monsters)
        {
            monster.Stop();
        }
    }

    void OnMonsterFinished(Monster monster) {
        _debuffsController.StartDebuff(monster.type);
        StartCoroutine(SpawnWithDelay(monster));
    }

    private void OnMonsterKilled(Monster monster) {
        monsterKilledEvent.Raise();
        StartCoroutine(SpawnWithDelay(monster));
    }

    public void StartSpawn() {
        if (_inProgress) return;
        _inProgress = true;
        
        var index = _random.Next(monsters.Count);
        monsters[index].Spawn();

        for (var i = 0; i < monsters.Count; i++) {
            if (i != index) StartCoroutine(SpawnWithDelay(monsters[i]));
        }
    }

    private IEnumerator SpawnWithDelay(Monster monster) {
        if (!_inProgress) yield break;
        
        yield return new WaitForSeconds(minDelay + (maxDelay - minDelay) * (float)_random.NextDouble());
        
        if (!_inProgress) yield break;

        if (monsters.Count(m => m.IsSpawned()) >= maxMonsters) {
            StartCoroutine(SpawnWithDelay(monster));
        } else {
            monster.Spawn();    
        }
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.M)) {
            StartSpawn();
        }
    }
}
