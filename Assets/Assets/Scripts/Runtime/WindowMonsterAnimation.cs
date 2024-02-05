using System;
using DigitalRuby.Tween;
using UnityEngine;

public class WindowMonsterAnimation : MonoBehaviour {

    [SerializeField]
    private GameObject window;
    
    [SerializeField]
    private GameObject monster;

    [SerializeField]
    protected float _harbingerThreshold = 0.33f;
    
    private float _startWindowY = 0;
    private float _startMonsterY = 0;

    private void Start() {
        _startWindowY = window.transform.localPosition.y;
        _startMonsterY = monster.transform.localPosition.y;

        var monsterComponent = GetComponent<Monster>();
        monsterComponent.progressUpdate += ProceedUpdate;
    }

    void ProceedUpdate(float progress) {
        if (progress < _harbingerThreshold) {
            window.transform.localPosition = new Vector3(window.transform.localPosition.x, _startWindowY + (progress / _harbingerThreshold), 0);
        }

        if (progress >= _harbingerThreshold) {
            monster.transform.localPosition = new Vector3(monster.transform.localPosition.x, _startMonsterY + ((progress - _harbingerThreshold) / (1 - _harbingerThreshold)) * 1.4f, 0);
        } else {
            monster.transform.localPosition = new Vector3(monster.transform.localPosition.x, _startMonsterY, 0);
        }
    }
}
