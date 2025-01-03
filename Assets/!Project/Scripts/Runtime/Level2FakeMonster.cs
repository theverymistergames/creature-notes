using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;

public class Level2FakeMonster : MonoBehaviour {
    [SerializeField] private float duration = 30f;
    private MonsterAnimation _animation;
    private MotionHandle _handle;

    private bool _stopped;
    
    void OnEnable() {
        _animation = GetComponent<MonsterAnimation>();
        
        //Animate();
        _stopped = false;
    }

    private void Animate() {
        _handle = LMotion.Create(0, 0.7f, duration).WithOnComplete(() => {
            _handle = LMotion.Create(.7f, 0, duration / 10).WithOnComplete(() => {
                if (_stopped) return;
                
                Animate();
            }).Bind(t => {
                _animation.ForceProceedUpdate(t);
            });;
        }).Bind(t => {
            _animation.ForceProceedUpdate(t);
        });
    }

    private void OnDisable() {
        _stopped = true;
        _handle.Cancel();
        _animation.ForceProceedUpdate(0);
    }
}
