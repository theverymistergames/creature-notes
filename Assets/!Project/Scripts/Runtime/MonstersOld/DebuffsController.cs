using System.Collections.Generic;
using LitMotion;
using UnityEngine;
using UnityEngine.UI;

public class DebuffsController : MonoBehaviour {
    
    [SerializeField] private List<Debuff> debuffs = new List<Debuff>();
    [SerializeField] private List<Sprite> monstersImages = new List<Sprite>();
    
    [SerializeField]
    private Image monsterImage;

    private void Start() {
        monsterImage.color = new Color(1, 1, 1, 0);
    }

    public void StartDebuff(MonsterType type) {
        var image = Instantiate(monsterImage, monsterImage.transform.parent);
        image.sprite = monstersImages[(int)type];
        image.color = new Color(1, 1, 1, 1);
        
        debuffs[(int)type].StartEffect();

        LMotion.Create(0f, 1f, 0.5f)
            .WithOnComplete(() => Destroy(image))
            .Bind(t => image.color = new Color(1f, 1f, 1f, 1f - t));
    }
}
