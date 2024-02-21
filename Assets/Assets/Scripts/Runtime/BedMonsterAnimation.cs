using System;
using UnityEngine;

public class BedMonsterAnimation : MonsterAnimation {
    [SerializeField]
    private GameObject bed;
    
    [SerializeField] private Vector3 finalMonsterPosition;
    
    private float _startBedX;
    private Vector3 _startMonsterPosition;

    private void Start() {
        SubscribeUpdate();
        
        monster.SetActive(false);
        
        _startBedX = bed.transform.localPosition.x;
        _startMonsterPosition = monster.transform.localPosition;
    }

    protected override void ProceedUpdate(float progress) {
        if (progress == 0) {
            monster.SetActive(false);
        }
        
        var localPosition = bed.transform.localPosition;
        localPosition.Set(_startBedX + MathF.Sin(Time.time * 70) * (progress > harbingerThreshold ? harbingerThreshold : progress) * 0.01f, localPosition.y, localPosition.z);
        bed.transform.localPosition = localPosition;

        if (progress >= harbingerThreshold) {
            monster.SetActive(true);
            monster.transform.localPosition = Vector3.Lerp(_startMonsterPosition, finalMonsterPosition, (progress - harbingerThreshold) / (1 - harbingerThreshold));
        } else {
            monster.transform.localPosition = _startMonsterPosition;
        }
    }
}
