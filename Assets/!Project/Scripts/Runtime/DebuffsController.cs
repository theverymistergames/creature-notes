using System;
using System.Collections;
using System.Collections.Generic;
using DigitalRuby.Tween;
using UnityEngine;
using UnityEngine.UI;

public class DebuffsController : MonoBehaviour {
    [SerializeField] private List<Debuff> debuffs = new List<Debuff>();
    
    [SerializeField] private List<Sprite> monstersImages = new List<Sprite>();
    
    [SerializeField]
    private Image monsterImage;

    private Tween<float> _currentImageTween;

    private void Start() {
        monsterImage.color = new Color(1, 1, 1, 0);
    }

    public void StartDebuff(MonsterType type) {
        // monsterImage.sprite = monstersImages[(int)type];

        var image = Instantiate(monsterImage, monsterImage.transform.parent);
        image.sprite = monstersImages[(int)type];
        image.color = new Color(1, 1, 1, 1);
        
        debuffs[(int)type].StartEffect();
        
        TweenFactory.Tween(null, 0, 1, 0.5f, TweenScaleFunctions.Linear,
            (tween) => {
                image.color = new Color(1, 1, 1, 1 - tween.CurrentProgress);
            }, tween => {
                Destroy(image);
            });
        
        // _currentImageTween = TweenFactory.Tween(null, 0, 1, 0.1f, TweenScaleFunctions.Linear,
        //     (t) => {
        //         monsterImage.color = new Color(1, 1, 1, t.CurrentProgress);
        //     }, t => {
        //         debuffs[(int)type].StartEffect();
        //
        //         _currentImageTween = TweenFactory.Tween(null, 0, 1, 1, TweenScaleFunctions.Linear,
        //             (tween) => {
        //                 monsterImage.color = new Color(1, 1, 1, 1 - tween.CurrentProgress);
        //             });
        //     });
    }
}
