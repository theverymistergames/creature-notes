using System;
using System.Collections.Generic;
using DigitalRuby.Tween;
using UnityEngine;

public class RunesStrikeContainer : MonoBehaviour {
    
    [NonSerialized]
    public bool collided = false;

    private Collider _collider;
    private List<GameObject> runes = new List<GameObject>();

    private Dictionary<MonsterType, int> dict = new Dictionary<MonsterType, int>();

    private void Start() {
        _collider = GetComponent<Collider>();
        
        dict.Add(MonsterType.Window, 0b00000001);
        dict.Add(MonsterType.Bed, 0b00000110);
        dict.Add(MonsterType.Closet, 0b00001001);
        dict.Add(MonsterType.Light, 0b00000100);
    }

    public void Strike() {
        foreach (Transform child in transform) {
            runes.Add(child.gameObject);
        }
    }

    private static void Test() {
        
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
            var monster = other.transform.GetComponent<Monster>();
        
            int result = 0;
        
            for (var i = 0; i < runes.Count; i++) {
                var rune = runes[i];
                var a = int.Parse((rune.name[4]).ToString());
                result |= (1 << a);
            }
            
            if (monster.IsSpawned() && dict[monster.type] == result) {
                monster.GetDamage();
        
                foreach (var rune in runes) {
                    rune.GetComponentInChildren<ParticleSystem>().Play();
                }
            }
        }
    }
}