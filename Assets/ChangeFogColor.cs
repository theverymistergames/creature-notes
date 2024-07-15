using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class ChangeFogColor : MonoBehaviour
{
    [SerializeField] private Volume forestVolume;
    [SerializeField] private Color color = Color.cyan;
    [SerializeField] private float time = 5;
    
    private Fog _fog;
    private Color _startFogColor;

    
    void Awake() {
        forestVolume.profile.TryGet(out _fog);
        _startFogColor = _fog.albedo.value;
    }

    public void Play() {
        StartCoroutine(Routine());
    }

    IEnumerator Routine() {
        var timer = 0f;
        
        while (timer < time) {
            var dt = Time.deltaTime;
            timer += dt;
            _fog.albedo.value = Color.Lerp(_startFogColor, color, timer / time);
            yield return new WaitForSeconds(dt);
        }
    }
}
