using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClosetMonsterAnimation : MonsterAnimation
{
    [SerializeField]
    private GameObject door;

    [SerializeField] private Vector3 finalMonsterPosition;
    
    [SerializeField] private float _finalDoorAngle = -70;
    
    private float _startDoorAngle = -70;
    private Vector3 _startMonsterPosition;

    private void Start() {
        SubscribeUpdate();
        
        monster.SetActive(false);
        
        _startDoorAngle = door.transform.eulerAngles.y;
        _startMonsterPosition = monster.transform.localPosition;
    }

   protected override void ProceedUpdate(float progress) {
       if (progress == 0) {
           monster.SetActive(false);
       }
       
        if (progress < harbingerThreshold) {
            door.transform.eulerAngles = new Vector3(door.transform.eulerAngles.x, _startDoorAngle + (progress / harbingerThreshold) * _finalDoorAngle, door.transform.eulerAngles.z);
        }

        if (progress >= harbingerThreshold) {
            monster.SetActive(true);
            monster.transform.localPosition = Vector3.Lerp(_startMonsterPosition, finalMonsterPosition, (progress - harbingerThreshold) / (1 - harbingerThreshold));
        } else {
            monster.transform.localPosition = _startMonsterPosition;
        }
    }
}
