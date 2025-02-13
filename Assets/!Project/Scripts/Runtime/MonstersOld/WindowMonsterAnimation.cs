using System;
using UnityEngine;

public class WindowMonsterAnimation : MonsterAnimation {

    [SerializeField]
    private GameObject window;
    
    private float _startWindowY;
    private float _startMonsterY;

    private void Start() {
        //SubscribeUpdate();
        
        monster.SetActive(false);
        
        _startWindowY = window.transform.localPosition.y;
        _startMonsterY = monster.transform.localPosition.y;
    }

    protected override void ProceedUpdate(float progress) {
        if (progress == 0) {
            monster.SetActive(false);
        }
        
        if (progress < harbingerThreshold) {
            window.transform.localPosition = new Vector3(window.transform.localPosition.x, _startWindowY + (progress / harbingerThreshold), 0);
        }

        if (progress >= harbingerThreshold) {
            monster.SetActive(true);
            monster.transform.localPosition = new Vector3(monster.transform.localPosition.x, _startMonsterY + ((progress - harbingerThreshold) / (1 - harbingerThreshold)) * 1.4f, 0);
        } else {
            monster.transform.localPosition = new Vector3(monster.transform.localPosition.x, _startMonsterY, 0);
        }
    }

#if UNITY_EDITOR
    [SerializeField] [Range(0f, 1f)] private float _testProgress;
    
    private void OnValidate() {
        //ProceedUpdate(_testProgress);
    }
#endif
}
