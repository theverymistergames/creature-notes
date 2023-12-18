using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RuneInput : MonoBehaviour {
    [SerializeField]
    private Image progressImage;
    
    [SerializeField]
    private Image runeImage;

    [SerializeField]
    private KeyCode key;

    [SerializeField]
    private float maxTime = 0.5f;
    
    [SerializeField]
    private int type;
    
    private float progress;
    private bool active;
    private bool runeActive;
    
    public delegate void RuneActivated(int type, bool active);
    public RuneActivated runeActivated;

    void Update()
    {
        if (Input.GetKeyDown(key)) {
            active = true;
        }

        if (!active) return;

        if (Input.GetKey(key)) {
            progress += Time.deltaTime / maxTime;
            
            if (progress >= 1) {
                UpdateRune();
                progress = 0;
                active = false;
            }
        }

        if (Input.GetKeyUp(key)) {
            progress = 0;
        }
        
        UpdateProgress();
    }

    void UpdateProgress() {
        progressImage.fillAmount = progress;
    }

    void UpdateRune() {
        runeActive = !runeActive;
        
        runeActivated.Invoke(type, runeActive);
        
        if (runeActive) {
            runeImage.color = new Color(1, 0.7f, 0.1f, 1);
        } else {
            runeImage.color = new Color(1, 1, 1, 0.1f);
        }
    }
}
