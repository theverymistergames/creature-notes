using System.Collections.Generic;
using MisterGames.Common.GameObjects;
using MisterGames.Scenario.Events;
using UnityEngine;

public sealed class ActivateGameObjectOnLevelsLoaded : MonoBehaviour, IEventListener {
    
    [SerializeField] private EventReference levelLoadedEvent;
    [SerializeField] private List<int> levels;
    [SerializeField] private GameObject go;
    [SerializeField] private bool _emissive;
    
    private float _startIntensity;
    private Color _startColor;
    private Material _material;
    private static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");
    private static readonly int EmissiveIntensity = Shader.PropertyToID("_EmissiveIntensity");

#if UNITY_EDITOR
    private string _path;
#endif
    
    private void Awake() {
#if UNITY_EDITOR
        _path = this.GetPathInScene();
#endif
        
        if (go == null) go = gameObject;
        
        if (_emissive) {
            _material = go.GetComponent<MeshRenderer>().material;
            _startIntensity = _material.GetFloat(EmissiveIntensity);
            _startColor = _material.GetColor(EmissiveColor);
        }
    }
    
    private void OnEnable() {
        SetActiveIfNeeded();
        levelLoadedEvent.Subscribe(this);
    }

    private void OnDisable() {
        levelLoadedEvent.Unsubscribe(this);
    }

    public void OnEventRaised(EventReference e) {
        SetActiveIfNeeded();
    }

    private void SetActiveIfNeeded() {
#if UNITY_EDITOR
        if (go == null) {
            Debug.LogError($"ActivateGameObjectOnLevelsLoaded[{_path}].SetActiveIfNeeded: f {Time.frameCount}, go is null");
            return;
        }  
#endif
        
        int count = levelLoadedEvent.GetCount();
        bool active = levels.Contains(count);
        
        if (_emissive) {
            _material.SetColor(EmissiveColor, active ? _startColor * _startIntensity : Color.black);
        } 
        else {
            go.SetActive(active);
        }
    }
}
