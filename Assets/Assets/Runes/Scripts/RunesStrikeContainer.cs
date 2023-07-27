using System;
using System.Collections.Generic;
using DigitalRuby.Tween;
using UnityEngine;

public class RunesStrikeContainer : MonoBehaviour {
    
    [NonSerialized]
    public bool collided = false;

    private Collider _collider;
    private List<GameObject> runes = new List<GameObject>();

    private void Start() {
        _collider = GetComponent<Collider>();
    }

    public void Strike() {
        foreach (Transform child in transform) {
            runes.Add(child.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (collided) return;
        if (other.CompareTag("Player")) return;
        
        collided = true;
        GetComponent<Collider>().enabled = false;
        
        foreach (var rune in runes) {
            var tween = TweenFactory.Tween(
                null,
                0,
                1,
                0.5f,
                TweenScaleFunctions.Linear,
                (t) => {
                    rune.GetComponent<Renderer>().material.SetFloat("_Alpha", 1 - t.CurrentProgress);
                });
        }
        
        if (other.transform.CompareTag("Enemy")) {
            var monster = other.transform.GetComponent<AbstractMonster>();
            
            if (monster.IsSpawned()) {
                monster.Damage();
        
                foreach (var rune in runes) {
                    rune.GetComponentInChildren<ParticleSystem>().Play();
                }
            }
        }
    }
}