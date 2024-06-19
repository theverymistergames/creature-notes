using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using MisterGames.Character.Core;
using MisterGames.Common.Attributes;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class ForestMonster : MonoBehaviour {
    [SerializeField] private GameObject head;
    [SerializeField] private ParticleSystem body;
    [SerializeField] private ParticleSystem death;
    [SerializeField] private Light light;
    [SerializeField] private float stopDistance = 20f;
    [SerializeField] private AudioSource monsterAudioSource;
    [SerializeField] private AudioClip deathAudio;
    
    public event Action Stopped = delegate {};
    
    private Transform _transform;
    private NavMeshAgent _agent;
    private AudioSource _source;

    private bool _stopped;
    
    void Start() {
        _agent = GetComponent<NavMeshAgent>();
        _source = GetComponent<AudioSource>();
    }

    private void OnEnable() {
        _transform = CharacterAccessRegistry.Instance.GetCharacterAccess().GetComponent<Transform>();
    }

    [Button]
    void Stop() {
        Stopped.Invoke();
        
        _agent.isStopped = true;
        _stopped = true;

        death.Play();
        body.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        var startScale = head.transform.localScale;
        var startRange = light.range;
        
        monsterAudioSource.PlayOneShot(deathAudio);
        
        LMotion.Create(1f, 0f, 3f).Bind(value => {
            head.transform.localScale = startScale * value;
            light.range = startRange * value;
            _source.volume = value;
        });
    }

    void Update() {
        if (!_transform || _stopped) return;
        
        var position = _transform.position;
        
        _agent.destination = position;
        
        var dir = position - transform.position + head.transform.position;
        var rot = Quaternion.LookRotation(dir);
        head.transform.rotation = Quaternion.Lerp(transform.rotation, rot, 5 * Time.deltaTime);

        var distance = GetPathRemainingDistance(_agent);

        // if (_agent.velocity != Vector3.zero) {
        //     _agent.transform.eulerAngles = new Vector3(0, Quaternion.LookRotation(_agent.velocity).eulerAngles.y, 0);            
        // }
        
        if (distance > stopDistance && distance < 10000f || distance == -1f && _agent.pathStatus != NavMeshPathStatus.PathComplete) {
            Stop();
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
