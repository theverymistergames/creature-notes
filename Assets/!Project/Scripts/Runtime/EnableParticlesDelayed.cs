using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableParticlesDelayed : MonoBehaviour {
    
    public float delay = 0.1f;

    private ParticleSystem _system;
    
    void Start() {
        _system = GetComponent<ParticleSystem>();
        _system.Stop();

        StartCoroutine(Test());
    }

    IEnumerator Test() {
        yield return new WaitForSeconds(delay);
        
        _system.Play();
    }

    void Update() {
        
    }
}
