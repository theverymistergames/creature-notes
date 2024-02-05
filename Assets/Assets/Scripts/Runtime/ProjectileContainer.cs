using System;
using System.Collections.Generic;
using DigitalRuby.Tween;
using UnityEngine;

public class ProjectileContainer : MonoBehaviour {
    
    [NonSerialized]
    public bool collided = false;

    private Collider _collider;
    private List<GameObject> runes = new List<GameObject>();

    [SerializeField]
    private ParticleSystem explosion;
    
    [SerializeField]
    private GameObject fireball;

    private Dictionary<MonsterType, int> dict = new Dictionary<MonsterType, int>();

    private List<int> _runesTypes;
    private Light _light;

    private void Start() {
        _collider = GetComponent<Collider>();
        _light = GetComponent<Light>();
        
        dict.Add(MonsterType.Window, 0b00000001);
        dict.Add(MonsterType.Bed, 0b00000110);
        dict.Add(MonsterType.Light, 0b00000100);
    }

    private void OnTriggerEnter(Collider other) {
        if (collided) return;
        if (other.CompareTag("Player")) return;
        
        collided = true;
        GetComponent<Collider>().enabled = false;

        if (other.transform.CompareTag("Enemy")) {
            var monster = other.transform.GetComponent<Monster>();

            //Debug.Log("POSHEL");
        
            
            if (monster.IsSpawned()) {
                monster.GetDamage();
            }
        }
        
        fireball.SetActive(false);
        explosion.Play();

        var intensity = _light.intensity;
        
        var tween = TweenFactory.Tween(
            null,
            0,
            1,
                .7f,
            TweenScaleFunctions.Linear,
            (t) => {
                _light.intensity = intensity * 2f - (t.CurrentProgress * intensity * 2f);
            },
            (t) => {
                Destroy(gameObject);
            });
        
    }
}
