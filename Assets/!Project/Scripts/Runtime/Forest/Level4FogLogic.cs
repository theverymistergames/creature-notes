using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class Level4FogLogic : MonoBehaviour {
    [SerializeField] private Volume forestVolume;
    [SerializeField] private float decreaseSpeed = 1;
    [SerializeField] private float increaseSpeed = 10;

    private Fog _fog;
    private float _startDistance;
    private bool _isDown;
    private float _currentDistance;

    public void SetIsDown(bool value) {
        _isDown = value;
    }
    
    void Awake() {
        forestVolume.profile.TryGet(out _fog);
        _startDistance = _fog.meanFreePath.value;
        _isDown = true;
        _currentDistance = _startDistance;
    }

    void Update() {
        _currentDistance += Time.deltaTime * (_isDown ? -decreaseSpeed : increaseSpeed);
        _currentDistance = Mathf.Clamp(_currentDistance, 1, _startDistance);
        _fog.meanFreePath.value = _currentDistance;
    }
}
