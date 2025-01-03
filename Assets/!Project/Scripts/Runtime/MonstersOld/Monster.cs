using System;
using LitMotion;
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
    private MotionHandle _damageTween;
    private float _progress;
    private bool _isNeedToUpdateProgress;
    private bool _finished;
    
    public delegate void MonsterKilled();
    public MonsterKilled monsterKilled;
    
    public delegate void MonsterFinished();
    public MonsterFinished monsterFinished;
    
    public delegate void ProgressUpdate(float progress);
    public ProgressUpdate progressUpdated;

    private MonsterAnimation _animation;

    private void Start() {
        _source = GetComponent<AudioSource>();
        _animation = GetComponent<MonsterAnimation>();
    }

    public bool IsSpawned() {
        return _spawned;
    }

    public void Spawn(float time, float threshold) {
        return;
        if (!_enabled) return;

        if (_source && type != MonsterType.Light) {
           _source.Play(); 
        }
        
        _animation.harbingerThreshold = threshold;
        _spawnTime = time;
        _spawned = true;
        _isNeedToUpdateProgress = true;
        _finished = false;
    }

    public void GetDamage() {
        return;
        if (_damageTween.IsActive()) _damageTween.Cancel();
        
        _isNeedToUpdateProgress = false;

        float startProgress = _progress;
        
        _damageTween = LMotion.Create(0f, 1f, 0.2f)
            .WithOnComplete(() => {
                if (_progress < 0.02f) monsterKilled.Invoke();
                Stop();
            })
            .Bind(t => _progress = startProgress - _damage * t);
    }

    public void Stop() {
        return;
        _spawned = false;
        _finished = false;
        _isNeedToUpdateProgress = false;
        _progress = 0;
        
        ProceedUpdate(_progress);
    }

    private void Finish() {
        if (_finished) return;
        
        ProceedUpdate(_progress);
        _finished = true;
        monsterFinished.Invoke();
    }

    private void Update() {
        return;
        if (!_spawned) return;
        
        if (_isNeedToUpdateProgress) _progress += Time.deltaTime / _spawnTime;

        if (_progress >= 1) {
            _progress = 1;
            Finish();
            return;
        }

        ProceedUpdate(_progress);
    }

    private void ProceedUpdate(float progress) {
        progressUpdated.Invoke(progress);
    }

    public bool IsFinished() {
        return _finished;
    }

    public bool IsEnabled() {
        return _enabled;
    }
}
