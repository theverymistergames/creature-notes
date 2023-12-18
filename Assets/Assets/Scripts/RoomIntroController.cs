using System.Collections;
using System.Collections.Generic;
using DigitalRuby.Tween;
using Kino.PostProcessing;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.PostProcessing;

public class RoomIntroController : MonoBehaviour {
    public static RoomIntroController instance;
    public int itemsCount = 2;
    
    public Volume v;
    private GrayScale _grayScale;
    private ColorAdjustments _adjustments;
    private Glitch _glitch;
    
    void Start() {
        if (!instance) {
            instance = this;
        }
        
        v = GetComponent<Volume>();
        v.profile.TryGet(out _grayScale);
        v.profile.TryGet(out _adjustments);
        v.profile.TryGet(out _glitch);
        
        _grayScale.intensity.value = 1f;
        _adjustments.postExposure.value = -2f;
        _glitch.block.value = 0.15f;
    }

    public void ItemWasSet() {
        itemsCount--;
        
        if (itemsCount <= 0) {
            TweenFactory.Tween(null, 0, 1, 2f, TweenScaleFunctions.Linear,
                (t) => {
                    _grayScale.intensity.value = 1 - t.CurrentProgress;
                    _adjustments.postExposure.value = -2 + t.CurrentProgress * 1;
                    _glitch.block.value = 0.15f - 0.15f * t.CurrentProgress;
                });
        }
    }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.T)) {
        //     _grayScale.intensity.value = 1f;
        // }
    }
}
