using System.Collections;
using System.Collections.Generic;
using DigitalRuby.Tween;
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
    
    void Start() {
        if (!instance) {
            instance = this;
        }
        
        v = GetComponent<Volume>();
        v.profile.TryGet(out _grayScale);
        v.profile.TryGet(out _adjustments);
        
        _grayScale.intensity.value = 1f;
        _adjustments.postExposure.value = -2f;
    }

    public void ItemWasSet() {
        itemsCount--;
        
        if (itemsCount <= 0) {
            TweenFactory.Tween(null, 0, 1, 2f, TweenScaleFunctions.Linear,
                (t) => {
                    _grayScale.intensity.value = 1 - t.CurrentProgress;
                    _adjustments.postExposure.value = -2 + t.CurrentProgress * 2;
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
