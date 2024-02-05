using System;
using DigitalRuby.Tween;
using UnityEngine;

public enum MonsterType {
    Window,
    Bed,
    Light,
}

public class Monster : MonoBehaviour {

    [SerializeField]
    protected float _spawnTime = 20;
    [SerializeField]
    protected float _damage = 0.4f;
    [SerializeField]
    protected bool _enabled = true;

    [SerializeField]
    public MonsterType type;

    [NonSerialized]
    private bool _spawned = false;
    private ITween _damageTween;
    private float _progress;
    private bool _active = false;
    
    public delegate void MonsterKilled();
    public MonsterKilled monsterKilled;
    
    public delegate void MonsterFinished();
    public MonsterFinished monsterFinished;
    
    public delegate void ProgressUpdate(float progress);
    public ProgressUpdate progressUpdate;

    public bool IsSpawned() {
        return _spawned;
    }

    public void Spawn() {
        if (!_enabled) return;
        
        _spawned = true;
        _active = true;
    }

    public void GetDamage() {
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

    private void Finish() {
        _spawned = false;
        _active = false;
        monsterFinished.Invoke();
        _progress = 0;

        ProceedUpdate(_progress);
    }

    private void Update() {
        if (!_spawned) return;
        
        if (_active) _progress += Time.deltaTime / _spawnTime;

        if (_progress >= 1) {
            _progress = 1;
            Finish();
            return;
        }

        ProceedUpdate(_progress);
    }

    private void ProceedUpdate(float progress) {
        progressUpdate.Invoke(progress);
    }
}
