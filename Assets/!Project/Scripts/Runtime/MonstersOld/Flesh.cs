using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Flesh : MonoBehaviour {
    public float[] positions;

    private int _currentIndex;
    void Start() {
        transform.localPosition = new Vector3(transform.localPosition.x, positions[0], transform.localPosition.z);
    }

    public void SetPosition(int index) {
        if (positions.Length <= index) return;
        
        if (index > _currentIndex) {
            SetPositionByIndex(index);
        } else {
            StopCoroutine(TweenPositionByIndex(_currentIndex));
            StartCoroutine(TweenPositionByIndex(index));
        }

        _currentIndex = index;
    }

    void SetPositionByIndex(int index) {
        transform.localPosition = new Vector3(transform.localPosition.x, positions[index], transform.localPosition.z);
    }

    private IEnumerator TweenPositionByIndex(int index) {
        var progress = 0f;
        var newPosition = new Vector3(transform.localPosition.x, positions[index], transform.localPosition.z);
        
        while (progress < 1) {
            var delta = Time.deltaTime;
            progress += delta;

            transform.localPosition = Vector3.Lerp(transform.localPosition, newPosition, progress);
            
            yield return new WaitForSeconds(delta);
        }
    }
}
