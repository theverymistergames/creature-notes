using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightMonsterAnimation : MonsterAnimation {
    [SerializeField]
    private GameObject lightSource;

    private AudioSource _audio;
    
    private float _targetTime;

    private void Start() {
        SubscribeUpdate();

        _audio = GetComponent<AudioSource>();
        monster.SetActive(false);
    }

    protected override void ProceedUpdate(float progress) {
        if (progress == 0) {
            lightSource.SetActive(true);
            monster.SetActive(false);
        }
        
        _targetTime -= Time.deltaTime;

        if (_targetTime > 0) return;
        
        if (progress >= harbingerThreshold) monster.SetActive(true);
            
        _targetTime = Random.Range(.2f, .5f) * (1 - progress / 2) + 5 * (1 - progress);

        StartCoroutine(BlinkRoutine(progress));
    }

    private IEnumerator BlinkRoutine(float progress) {
        lightSource.SetActive(false);
        
        yield return new WaitForSeconds(0.2f - 0.1f * progress);
        
        _audio.Play();
        
        lightSource.SetActive(true);
        monster.SetActive(false);
    }
}
