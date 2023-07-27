using System;
using DigitalRuby.Tween;
using UnityEngine;

public abstract class AbstractMonster : MonoBehaviour {

    [SerializeField]
    protected float _spawnTime = 20;
    [SerializeField]
    protected float _harbingerThreshold = 0.33f;
    [SerializeField]
    protected float _damage = 0.4f;

    [NonSerialized]
    private bool _spawned = false;
    private ITween _damageTween;
    private float _progress;
    private bool _active = false;
    private bool _finished = false;
    
    public delegate void MonsterKilled();
    public MonsterKilled monsterKilled;
    
    public delegate void MonsterFinished();
    public MonsterFinished monsterFinished;
    
    public bool IsSpawned() {
        return _spawned;
    }

    public void Spawn() {
        _finished = false;
        _spawned = true;
        _active = true;
    }

    public void Damage() {
        if (_damageTween is { State: TweenState.Running }) _damageTween.Stop(TweenStopBehavior.Complete);
        
        _active = false;

        var startProgress = _progress;
        
        _damageTween = TweenFactory.Tween(null, 0, 1, 0.2f, TweenScaleFunctions.Linear,
            (t) => {
                if (_progress <= 0) {
                    t.Stop(TweenStopBehavior.DoNotModify);
                    _spawned = false;
                    ProceedUpdate(0);
                    monsterKilled.Invoke();
                    return;
                }

                _progress = startProgress - _damage * t.CurrentProgress;
            },
            tween => {
                _active = true;
            });
    }

    private void Update() {
        // if (Input.GetKeyDown(KeyCode.M)) {
        //     Spawn();
        // }

        if (!_spawned) return;
        
        if (_active) _progress += Time.deltaTime / _spawnTime;

        if (_progress >= 1) {
            _progress = 1;
            if (!_finished) monsterFinished.Invoke();
            _finished = true;
            return;
        }

        ProceedUpdate(_progress);
    }

    protected abstract void ProceedUpdate(float progress);
}