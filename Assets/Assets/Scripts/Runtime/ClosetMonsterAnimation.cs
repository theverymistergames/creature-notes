using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClosetMonsterAnimation : MonoBehaviour
{
    [SerializeField]
    private GameObject door;
    
    [SerializeField]
    private GameObject monster;

    [SerializeField] private Vector3 finalMonsterPosition;
    
    [SerializeField] private float _finalDoorAngle = -70;

    [SerializeField]
    protected float _harbingerThreshold = 0.33f;
    
    private float _startDoorAngle = -70;
    private Vector3 _startMonsterPosition;

    private void Start() {
        _startDoorAngle = door.transform.eulerAngles.y;
        _startMonsterPosition = monster.transform.localPosition;

        var monsterComponent = GetComponent<Monster>();
        monsterComponent.progressUpdate += ProceedUpdate;
    }

   void ProceedUpdate(float progress) {
        if (progress < _harbingerThreshold) {
            door.transform.eulerAngles = new Vector3(door.transform.eulerAngles.x, _startDoorAngle + (progress / _harbingerThreshold) * _finalDoorAngle, door.transform.eulerAngles.z);
        }

        if (progress >= _harbingerThreshold) {
            monster.transform.localPosition = Vector3.Lerp(_startMonsterPosition, finalMonsterPosition, (progress - _harbingerThreshold) / (1 - _harbingerThreshold));
        } else {
            monster.transform.localPosition = _startMonsterPosition;
        }
    }
}
