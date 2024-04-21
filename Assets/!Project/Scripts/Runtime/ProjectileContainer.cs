using System;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;

public class ProjectileContainer : MonoBehaviour {

    public float speedMultiplier = 0.05f;
    
    [NonSerialized]
    public bool collided = false;


    [SerializeField]
    private ParticleSystem explosion;
    
    [SerializeField]
    private GameObject fireball;

    private List<int> _runesTypes;
    private Light _light;

    private bool _striked;
    private Vector3 _direction;
    private AudioSource explosionSound;

    private void Start() {
        explosionSound = GetComponent<AudioSource>();
        _light = GetComponent<Light>();

        var main = explosion.main;
        main.startSize = new ParticleSystem.MinMaxCurve(0, 0);
    }

    public void Strike(Vector3 dir) {
        _direction = dir;
        _striked = true;
    }

    private void Update() {
        if (_striked && !collided) {
            transform.position += _direction * speedMultiplier;
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (collided) return;
        if (other.CompareTag("Player")) return;
        
        collided = true;
        GetComponent<Collider>().enabled = false;

        if (other.transform.CompareTag("Enemy")) {
            var monster = other.transform.GetComponent<Monster>();
            
            if (monster.IsSpawned()) {
                var main = explosion.main;
                main.startSize = new ParticleSystem.MinMaxCurve(.5f, .5f);
                
                monster.GetDamage();
            }
        }
        
        explosionSound.Play();
        
        fireball.SetActive(false);
        explosion.Play();

        float intensity = _light.intensity;
        
        LMotion.Create(0f, 1f, 2f)
            .WithOnComplete(() => Destroy(gameObject))
            .Bind(t => _light.intensity = intensity * 2f - t * intensity * 2f);
        
    }
}
