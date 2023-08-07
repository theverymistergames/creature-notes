using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightMonsterAnimation : MonoBehaviour
{
    [SerializeField]
    private GameObject lightSource;
    
    [SerializeField]
    private GameObject monster;

    [SerializeField]
    protected float _harbingerThreshold = 0.33f;

    
    private float _targetTime;

    private void Start() {
        _targetTime = Random.Range(1, 1.5f);
        monster.SetActive(false);

        var monsterComponent = GetComponent<Monster>();
        monsterComponent.progressUpdate += ProceedUpdate;
    }

    void ProceedUpdate(float progress) {
        if (progress == 0) {
            lightSource.SetActive(true);
            monster.SetActive(false);
        }
        
        _targetTime -= Time.deltaTime;

        if (_targetTime <= 0) {
            lightSource.SetActive(true);
            if (progress >= _harbingerThreshold) monster.SetActive(true);
            
            _targetTime = Random.Range(0.5f - 0.4f * progress, 1.5f - 1.2f * progress);

            StartCoroutine(BlinkRoutine(progress));
        }
    }

    private IEnumerator BlinkRoutine(float progress) {
        yield return new WaitForSeconds(0.2f - 0.1f * progress);
        
        lightSource.SetActive(false);
        monster.SetActive(false);
    }
}
