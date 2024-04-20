using System.Collections;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.Serialization;

public class MatrixTransition : MonoBehaviour {
    public GameObject element;
    public float maxCount = 300;
    public int emission = 10;
    public float speed = 0.2f;
    public float yOffset = 500;

    private RectTransform _rootTransform;

    private List<RectTransform> _particles = new();

    private float _width, _height;
    
    void Start() {
        _rootTransform = GetComponent<RectTransform>();
        _width = _rootTransform.rect.width;
        _height = _rootTransform.rect.height;
        
        element.SetActive(false);
    }

    void SpawnParticles(int count) {
        for (var i = 0; i < count; i++) {
            var el = Instantiate(element, gameObject.transform);
            el.SetActive(true);
            var rectTransform = el.GetComponent<RectTransform>();
        
            var startX = Random.Range(-_width / 2, _width / 2);
            var startX2 = Mathf.Sin(Time.time * 30) * _width;
        
            rectTransform.localPosition = new Vector3(startX2, _height / 2 + yOffset, 0);
        
            _particles.Add(rectTransform);
        }
    }

    void Update() {
        if (_particles.Count < maxCount) {
            SpawnParticles(emission);
        }

        foreach (var particle in _particles) {
            particle.localPosition += Vector3.down * speed;
            
            if (particle.localPosition.y <= -_rootTransform.rect.height / 2 - yOffset) {
                var startX2 = Mathf.Sin(Time.time * 30) * _width;
                // particle.localPosition = new Vector3(Random.Range(-_width / 2, _width / 2), _height / 2 + yOffset, 0);
                particle.localPosition = new Vector3(startX2, _height / 2 + yOffset, 0);
            }
        }
    }
}
