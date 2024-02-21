using System;
using System.Collections.Generic;
using DigitalRuby.Tween;
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
                monster.GetDamage();
            }
        }
        
        explosionSound.Play();
        
        fireball.SetActive(false);
        explosion.Play();

        var intensity = _light.intensity;
        
        var tween = TweenFactory.Tween(
            null,
            0,
            1,
                2,
            TweenScaleFunctions.Linear,
            (t) => {
                _light.intensity = intensity * 2f - (t.CurrentProgress * intensity * 2f);
            },
            (t) => {
                Destroy(gameObject);
            });
        
    }
}
