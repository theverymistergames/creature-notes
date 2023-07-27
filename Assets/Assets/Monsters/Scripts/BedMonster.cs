using System;
using UnityEngine;

public class BedMonster : AbstractMonster {
    [SerializeField]
    private GameObject bed;
    
    [SerializeField]
    private GameObject monster;

    [SerializeField] private Vector3 finalMonsterPosition;
    
    private float _startBedX = 0;
    private Vector3 _startMonsterPosition;

    private void Start() {
        _startBedX = bed.transform.localPosition.x;
        _startMonsterPosition = monster.transform.localPosition;
    }

    protected override void ProceedUpdate(float progress) {
        var localPosition = bed.transform.localPosition;
        localPosition = new Vector3(_startBedX + MathF.Sin(Time.time * 100) * (progress > _harbingerThreshold ? _harbingerThreshold : progress) * 0.1f, localPosition.y, localPosition.z);
        bed.transform.localPosition = localPosition;

        if (progress >= _harbingerThreshold) {
            monster.transform.localPosition = Vector3.Lerp(_startMonsterPosition, finalMonsterPosition, (progress - _harbingerThreshold) / (1 - _harbingerThreshold));
        } else {
            monster.transform.localPosition = _startMonsterPosition;
        }
    }
}