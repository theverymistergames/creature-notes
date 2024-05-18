using System;
using UnityEngine;

namespace MisterGames.BlueprintLib {
    public class MusicBoxRoll : MonoBehaviour {
        private bool _playing = false;
        private float _timer = 0;
        private float _totalDuration = 0;
        private Quaternion _startRotation;

        private void Start() {
            _totalDuration = GetComponent<AudioSource>().clip.length;
        }

        public void Play() {
            _timer = 0;
            _playing = true;
            _startRotation = transform.localRotation;
        }
        
        public void Stop() {
            _playing = false;
            transform.localRotation = _startRotation;
        }

        private void Update() {
            if (_playing) {
                _timer += Time.deltaTime;
                //todo rotation speed
                transform.localRotation *= Quaternion.Euler(0, -0.2f, 0);
            }
        }
    }
}