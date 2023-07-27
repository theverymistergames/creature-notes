using System.Collections;
using System.Collections.Generic;
using DigitalRuby.Tween;
using UnityEngine;

public class RunesCaster : MonoBehaviour {
    
    [SerializeField]
    private float _strikeSpeed = 0.1f;
    
    [SerializeField]
    private GameObject StrikeContainerPrefab;
    
    [SerializeField]
    private GameObject[] RunesPrefabs;

    private List<int> BusyRunesIndices = new List<int>();
    private Dictionary<GameObject, ITween> _runesTweensMap = new Dictionary<GameObject, ITween>();
    private List<GameObject> _currentRunes = new List<GameObject>();

    void Start()
    {
        TweenFactory.ClearTweensOnLevelLoad = true;
        TweenFactory.Clear();
    }

    void StrikeRunes() {
        if (_currentRunes.Count == 0) return;
        
        BusyRunesIndices.Clear();
        
        var container = Instantiate(StrikeContainerPrefab, gameObject.transform);
        container.transform.localPosition = new Vector3(0, 0, 0.5f);
        container.transform.SetParent(null);

        var strikeContainer = container.GetComponent<RunesStrikeContainer>();
        
        _currentRunes.ForEach(rune => {
            if (_runesTweensMap.ContainsKey(rune)) {
                _runesTweensMap[rune].Stop(TweenStopBehavior.DoNotModify);
            }
            
            rune.transform.SetParent(container.transform);
        });
        
        _currentRunes.Clear();
        
        strikeContainer.Strike();
        
        var currentDirection = (container.transform.position - gameObject.transform.position).normalized;
        
        var tween = TweenFactory.Tween(
            null,
            0,
            1,
            5,
            TweenScaleFunctions.QuadraticEaseIn,
            (t) => {
                container.transform.position += currentDirection * _strikeSpeed;
                if (strikeContainer.collided) t.Stop(TweenStopBehavior.DoNotModify);
            },
            tween => {
                
            });
    }

    void ResetRunes() {
        if (_currentRunes.Count == 0) return;
        
        BusyRunesIndices.Clear();
        
        _currentRunes.ForEach(rune => {
            if (_runesTweensMap.ContainsKey(rune)) {
                _runesTweensMap[rune].Stop(TweenStopBehavior.DoNotModify);
            }

            TweenFactory.Tween(
                null,
                .5f,
                0,
                0.2f,
                TweenScaleFunctions.QuadraticEaseIn,
                (t) => {
                    rune.transform.localPosition = new Vector3(0, 0, t.CurrentValue);
                    rune.GetComponent<Renderer>().material.SetFloat("_Alpha", 1 - t.CurrentProgress);
                },
                tween => {
                    Destroy(rune);
                });
        });
        
        _currentRunes.Clear();
    }

    void Update() {
        var index = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) index = 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) index = 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) index = 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) index = 3;

        if (index >=  0 && !BusyRunesIndices.Contains(index)) {
            BusyRunesIndices.Add(index);

            var rune = Instantiate(RunesPrefabs[index], gameObject.transform);
            rune.GetComponent<Renderer>().material.SetFloat("_Alpha", 0);
            _currentRunes.Add(rune);

            TweenFactory.Tween(null, 0, 1, 0.3f, TweenScaleFunctions.CubicEaseOut,
                (t) => {
                    rune.GetComponent<Renderer>().material.SetFloat("_Alpha", t.CurrentProgress);
                });

            var tween = TweenFactory.Tween(null, 0, 0.5f, 0.5f, TweenScaleFunctions.CubicEaseOut,
                (t) => {
                    rune.transform.localPosition = new Vector3(0, 0, t.CurrentValue);
                },
                tween => {
                    _runesTweensMap.Remove(rune);
                });

            _runesTweensMap.Add(rune, tween);
        }
            
        if (Input.GetKeyDown(KeyCode.R) || Input.GetMouseButtonDown(1)) {
            ResetRunes();
        }
            
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) {
            StrikeRunes();
        }
    }
}
