using System.Collections;
using System.Collections.Generic;
using DigitalRuby.Tween;
using UnityEngine;

public class MagicCaster : MonoBehaviour {
    
    [SerializeField] private float _strikeSpeed = 0.1f;
    
    [SerializeField] private GameObject StrikeContainerPrefab;
    
    [SerializeField] private GameObject FireballPrefab;
    
    [SerializeField] private GameObject[] RunesPrefabs;

    [SerializeField] private RuneInputsManager _inputsManager;

    void Start() {
        TweenFactory.ClearTweensOnLevelLoad = true;
        TweenFactory.Clear();
    }

    void StrikeRunes() {
        var container = Instantiate(StrikeContainerPrefab, gameObject.transform);
        container.transform.localPosition = new Vector3(0, 0, 0.1f);
        container.transform.SetParent(null);

        // var fireball = Instantiate(FireballPrefab, container.transform);
        // fireball.transform.position = new Vector3(0, -0.5f, 0);

        var strikeContainer = container.GetComponent<ProjectileContainer>();
        
        var currentDirection = (container.transform.position - gameObject.transform.position).normalized;
        
        TweenFactory.Tween(null, 0, 1, 5, TweenScaleFunctions.QuadraticEaseIn,
            (t) => {
                container.transform.position += currentDirection * _strikeSpeed;
                if (strikeContainer.collided) t.Stop(TweenStopBehavior.DoNotModify);
            });
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) {
            StrikeRunes();
        }
    }
}
