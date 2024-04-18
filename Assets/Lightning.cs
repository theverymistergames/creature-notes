using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

public class Lightning : MonoBehaviour {
    [SerializeField] private Light _light;
    
    void Start() {
        _light.enabled = false;
    }

    [Button]
    public void Blink() {
        BlinkSequence();
    }

    async void BlinkSequence() {
        _light.enabled = true;

        await UniTask.Delay(TimeSpan.FromSeconds(.03f + Random.Range(0.1f, 0.2f)));
        
        _light.enabled = false;
        
        await UniTask.Delay(TimeSpan.FromSeconds(.03f));
        
        _light.enabled = true;

        await UniTask.Delay(TimeSpan.FromSeconds(.05f + Random.Range(0, 0.1f)));
        
        _light.enabled = false;
    }
}
