using System;
using DigitalRuby.Tween;
using UnityEngine;

public class WindowMonster : AbstractMonster {

    [SerializeField]
    private GameObject window;
    
    [SerializeField]
    private GameObject monster;
    
    private float _startWindowY = 0;
    private float _startMonsterY = 0;

    private void Start() {
        _startWindowY = window.transform.localPosition.y;
        _startMonsterY = monster.transform.localPosition.y;
    }

    protected override void ProceedUpdate(float progress) {
        if (progress < _harbingerThreshold) {
            window.transform.localPosition = new Vector3(window.transform.localPosition.x, _startWindowY + (progress / _harbingerThreshold), 0);
        }

        if (progress >= _harbingerThreshold) {
            monster.transform.localPosition = new Vector3(monster.transform.localPosition.x, _startMonsterY + ((progress - _harbingerThreshold) / (1 - _harbingerThreshold)) * 1.4f, 0);
        }
    }
}