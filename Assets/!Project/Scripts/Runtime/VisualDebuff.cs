using UnityEngine;
using UnityEngine.UI;

public class VisualDebuff : Debuff {
    [SerializeField]
    private float _effectTime = 20;
    private float _progress;
    private Image _image;
    private bool _active;
    
    void Start() {
        _image = GetComponent<Image>();
        _image.color = new Color(1, 1, 1, 0);
    }

    public override void StartEffect() {
        _active = true;
        _progress = 0;
    }

    void Update()
    {
        if (_active) {
            _progress += Time.deltaTime / _effectTime;
            if (_progress >= 1) _progress = 1;
            
            _image.color = new Color(1, 1, 1, 1 - _progress);

            if (_progress >= 1) _active = false;
        }
    }
}
