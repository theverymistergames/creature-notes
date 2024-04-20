using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnvironmentController : MonoBehaviour {
    [SerializeField] private Lightning lightning;
    [SerializeField] private AudioClip[] clips;

    private AudioSource _audioSource;
    
    private void Start() {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.Play();
    }

    private void OnEnable() {
        LightningSequence();
    }

    async void LightningSequence() {
        await UniTask.Delay( Random.Range(5000, 10000));
        
        lightning.Blink();
        
        await UniTask.Delay(Random.Range(1000, 5000));

        var id = Random.Range(0, clips.Length - 1);
        _audioSource.PlayOneShot(clips[id]);
        
        LightningSequence();
    }
}
