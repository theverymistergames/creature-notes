using System;
using System.Collections;
using System.Collections.Generic;
using MisterGames.Interact.Interactives;
using UnityEngine;
using Random = UnityEngine.Random;

public class Ball : MonoBehaviour
{
    private Interactive _interactive;
    private Rigidbody _rb;
    
    void Start() {
        _rb = GetComponent<Rigidbody>();
        _interactive = GetComponent<Interactive>();
        _interactive.OnStartInteract += PushBall;
    }

    private void PushBall(IInteractiveUser user) {
        _rb.AddRelativeForce(Random.onUnitSphere * Random.Range(100, 200));
    }
}
