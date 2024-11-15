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
    
    public event Action OnStop = delegate {};
    public bool IsStopped { get; private set; }

    private Transform _transform;
    private NavMeshAgent _agent;
    private AudioSource _source;

    private Vector3 _startHeadScale;
    private float _startLightRange;

    private void Awake() {
        _agent = GetComponent<NavMeshAgent>();
        _source = GetComponent<AudioSource>();
        _startHeadScale = head.transform.localScale;
        _startLightRange = light.range;
    }

    private void OnEnable() {
        _transform = CharacterSystem.Instance.GetCharacter().GetComponent<Transform>();
        IsStopped = false;
        _agent.isStopped = false;

        head.transform.localScale = _startHeadScale;
        _source.volume = 1;
        light.range = _startLightRange;
    }

    private void Stop(bool caught = false) {
        if (!caught) OnStop.Invoke();
        
        _agent.isStopped = true;
        IsStopped = true;

        death.Play();
        body.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        
        monsterAudioSource.PlayOneShot(deathAudio);
        
        LMotion.Create(1f, 0f, 3f).Bind(value => {
            head.transform.localScale = _startHeadScale * value;
            light.range = _startLightRange * value;
            _source.volume = value;
        });
    }

    private void Update() {
        if (IsStopped) return;

        _agent.SetDestination(_transform.position);
        
        float distance = GetPathRemainingDistance(_agent);

        if (distance > stopDistance && distance < 10000f || distance < 0f && _agent.pathStatus != NavMeshPathStatus.PathComplete) {
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
        {
            return -1f;
        }

        float distance = 0.0f;
        
        for (int i = 0; i < navMeshAgent.path.corners.Length - 1; i++) {
            var path = navMeshAgent.path;
            distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        return distance;
    }
}
