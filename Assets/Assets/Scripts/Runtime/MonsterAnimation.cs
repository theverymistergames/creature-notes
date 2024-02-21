using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class MonsterAnimation : MonoBehaviour
{
    [SerializeField]
    protected GameObject monster;

    [SerializeField]
    public float harbingerThreshold = 0.8f;

    protected void SubscribeUpdate() {
        var monsterComponent = GetComponent<Monster>();
        monsterComponent.progressUpdate += ProceedUpdate;
    }

    protected abstract void ProceedUpdate(float progress);
}
