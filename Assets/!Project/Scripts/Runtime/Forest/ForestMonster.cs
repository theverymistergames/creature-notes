using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using MisterGames.Character.Core;
using MisterGames.Common.Attributes;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class ForestMonster : MonoBehaviour {
    [SerializeField] private GameObject head;
    [SerializeField] private ParticleSystem body;
    [SerializeField] private ParticleSystem death;
    [SerializeField] private Light light;
    [SerializeField] private float stopDistance = 20f;
    [Header("Sounds")]
    [SerializeField] private AudioSource monsterAudioSource;
    [SerializeField] private AudioClip deathAudio;
    [Header("Caught")]
    [SerializeField] private EventReference caughtEvent;
    [SerializeField] private float caughtDistance = 2f;
    
    public event Action Stopped = delegate {};
    
    private Transform _transform;
    private NavMeshAgent _agent;
    private AudioSource _source;

    private bool _stopped;
    private Vector3 _startHeadScale;
    private float _startLightRange;
    
    void Awake() {
        _agent = GetComponent<NavMeshAgent>();
        _source = GetComponent<AudioSource>();
        _startHeadScale = head.transform.localScale;
        _startLightRange = light.range;
    }

    private void OnEnable() {
        _transform = CharacterSystem.Instance.GetCharacter().GetComponent<Transform>();
        _stopped = false;
        _agent.isStopped = false;

        head.transform.localScale = _startHeadScale;
        _source.volume = 1;
        light.range = _startLightRange;
    }

    void Stop(bool caught = false) {
        if (!caught) Stopped.Invoke();
        
        _agent.isStopped = true;
        _stopped = true;

        death.Play();
        body.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        
        monsterAudioSource.PlayOneShot(deathAudio);
        
        LMotion.Create(1f, 0f, 3f).Bind(value => {
            head.transform.localScale = _startHeadScale * value;
            light.range = _startLightRange * value;
            _source.volume = value;
        });
    }

    void Update() {
        if (!_transform || _stopped) return;
        
        var position = _transform.position;
        
        _agent.destination = position;
        
        // var dir = position - transform.position + head.transform.position;
        // var rot = Quaternion.LookRotation(dir);
        // head.transform.rotation = Quaternion.Lerp(transform.rotation, rot, 5 * Time.deltaTime);

        var distance = GetPathRemainingDistance(_agent);

        // if (_agent.velocity != Vector3.zero) {
        //     _agent.transform.eulerAngles = new Vector3(0, Quaternion.LookRotation(_agent.velocity).eulerAngles.y, 0);            
        // }
        
        if (distance > stopDistance && distance < 10000f || distance == -1f && _agent.pathStatus != NavMeshPathStatus.PathComplete) {
            Stop();
        } else if (distance < caughtDistance && distance > 0) {
            Stop(true);
            caughtEvent.Raise();
        }
    }

    private static float GetPathRemainingDistance(NavMeshAgent navMeshAgent) {
        if (navMeshAgent.pathPending ||
            navMeshAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
            navMeshAgent.path.corners.Length == 0)
            return -1f;

        var distance = 0.0f;
        
        for (var i = 0; i < navMeshAgent.path.corners.Length - 1; ++i) {
            var path = navMeshAgent.path;
            distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        return distance;
    }
}
