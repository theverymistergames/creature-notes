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
    protected float _damage = 1;
    [SerializeField]
    protected bool _enabled = true;

    [SerializeField]
    public MonsterType type;

    private AudioSource _source;

    [NonSerialized]
    private float _spawnTime = 40;
    private bool _spawned;
    private ITween _damageTween;
    private float _progress;
    private bool _active;
    
    public delegate void MonsterKilled();
    public MonsterKilled monsterKilled;
    
    public delegate void MonsterFinished();
    public MonsterFinished monsterFinished;
    
    public delegate void ProgressUpdate(float progress);
    public ProgressUpdate progressUpdate;

    private MonsterAnimation _animation;

    private void Start() {
        _source = GetComponent<AudioSource>();
        _animation = GetComponent<MonsterAnimation>();
    }

    public bool IsSpawned() {
        return _spawned;
    }

    public void Spawn(float time, float threshold) {
        if (!_enabled) return;

        if (_source && type != MonsterType.Light) {
           _source.Play(); 
        }
        
        _animation.harbingerThreshold = threshold;
        _spawnTime = time;
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

    public void Stop() {
        _spawned = false;
        _active = false;
        _progress = 0;
        ProceedUpdate(_progress);
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

    public bool isEnabled()
    {
        return _enabled;
    }
}
