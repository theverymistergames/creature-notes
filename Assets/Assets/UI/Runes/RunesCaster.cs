using System.Collections;
using System.Collections.Generic;
using DigitalRuby.Tween;
using UnityEngine;

public class RunesCaster : MonoBehaviour {
    
    [SerializeField]
    private GameObject[] RunesGameObjects;

    private List<Rune> Runes = new List<Rune>();
    private List<ITween<float>> Tweens = new List<ITween<float>>();

    class Rune {
        public GameObject runeGO;
        public bool busy;
    }
    
    void Start()
    {
        TweenFactory.ClearTweensOnLevelLoad = true;
        TweenFactory.Clear();

        foreach (var runeGO in RunesGameObjects) {
            var rune = new Rune();
            
            rune.busy = false;
            rune.runeGO = runeGO;
            
            Runes.Add(rune);
        }

        ResetRunes(true);
    }

    void StrikeRunes() {
        Runes.ForEach(rune => {
            if (rune.busy) {
                System.Action<ITween<float>> progressCallback = (t) => {
                    rune.runeGO.transform.localPosition = new Vector3(0, 0, t.CurrentValue);
                    // rune.runeGO.GetComponent<Renderer>().material.SetFloat("_Alpha", 1 - t.CurrentProgress);
                };

                TweenFactory.Tween(
                    null,
                    0,
                    5,
                    1f,
                    TweenScaleFunctions.CubicEaseOut,
                    progressCallback, tween => {
                        rune.runeGO.transform.localPosition = new Vector3(0, 0, -0.5f);
                        rune.runeGO.GetComponent<Renderer>().material.SetFloat("_Alpha", 0);
                        rune.busy = false;
                    });
            }
        });
    }

    void ResetRunes(bool immediately) {
        Runes.ForEach(rune => {
            if (!immediately && rune.busy) {
                System.Action<ITween<float>> progressCallback = (t) => {
                    rune.runeGO.transform.localPosition = new Vector3(0, 0, t.CurrentValue);
                    rune.runeGO.GetComponent<Renderer>().material.SetFloat("_Alpha", 1 - t.CurrentProgress);
                };

                TweenFactory.Tween(
                    null,
                    0,
                    -.5f,
                    0.2f,
                    TweenScaleFunctions.QuadraticEaseIn,
                    progressCallback, tween => {
                        rune.busy = false;
                    });
            }
            else {
                rune.runeGO.transform.localPosition = new Vector3(0, 0, -0.5f);
                rune.runeGO.GetComponent<Renderer>().material.SetFloat("_Alpha", 0);
                rune.busy = false;
            }
        });
    }

    void Update() {
        Rune rune = new Rune();
            
        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            rune = Runes[0];
        }
            
        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            rune = Runes[1];
        }
            
        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            rune = Runes[2];
        }
            
        if (Input.GetKeyDown(KeyCode.Alpha4)) {
            rune = Runes[3];
        }

        if (rune.runeGO != null && !rune.busy) {
            rune.busy = true;
                
            System.Action<ITween<float>> progress = (t) => {
                rune.runeGO.transform.localPosition = new Vector3(0, 0, t.CurrentValue);
                rune.runeGO.GetComponent<Renderer>().material.SetFloat("_Alpha", t.CurrentProgress);
            };

            var tween = TweenFactory.Tween(
                null,
                -0.5f,
                0,
                0.5f,
                TweenScaleFunctions.CubicEaseOut,
                progress, tween => {
                    Tweens.Remove(tween);
                });
            
            Tweens.Add(tween);
        }
            
        if (Input.GetKeyDown(KeyCode.R)) {
            if (Tweens.Count == 0) {
                ResetRunes(false);
            }
        }
            
        if (Input.GetKeyDown(KeyCode.E)) {
            if (Tweens.Count == 0) {
                StrikeRunes();
            }
        }
    }
}
