using System;
using System.Collections.Generic;
using System.Globalization;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Attributes;
using MisterGames.Tick.Core;
using UnityEngine;

namespace MisterGames {
    
    public sealed class GyroscopeReader : MonoBehaviour {

        [SerializeField] private TextAsset _textAsset;
        [SerializeField] [ReadOnly] private List<Record> _records;
        [SerializeField] [Min(0.001f)] private float _smoothing;
        [SerializeField] private bool _start;
        
        [Serializable]
        private struct Record {
            public float time;
            public Vector3 eulers;
        }
        
        private async void OnActivate() {
            var timeSource = TimeSources.Get(PlayerLoopStage.Update);
            float timer = 0f;
            int index = 0;
            int count = _records.Count;

            var currentRotation = transform.rotation;
            var targetRotation = currentRotation;
            
            while (true) {
                float dt = timeSource.DeltaTime;
                timer += dt;
                var velocity = Vector3.zero;
                
                while (true) {
                    if (index > count - 1) return;
                    
                    var record = _records[index];
                    
                    if (record.time < timer) {
                        index++;
                        continue;
                    }

                    if (index < count - 1) {
                        var next = _records[index + 1];
                        if (next.time >= timer) velocity = Vector3.Lerp(record.eulers, next.eulers, (timer - record.time) / (next.time - record.time));
                    }
                    
                    break;
                }

                targetRotation *= Quaternion.Euler(Mathf.Rad2Deg * velocity * dt);
                currentRotation = Quaternion.Slerp(currentRotation, targetRotation, _smoothing * dt);

                transform.rotation = currentRotation;
                
                await UniTask.Yield();
            }
        }

        private void OnValidate() {
            _records.Clear();
            
            if (_textAsset == null) return;
            
            string text = _textAsset.text;
            string[] lines = text.Split("\r\n");
            
            for (int i = 1; i < lines.Length - 1; i++) {
                string line = lines[i];
                string[] items = line.Split(",");

                _records.Add(new Record {
                    time = float.Parse(items[1], CultureInfo.InvariantCulture.NumberFormat),
                    eulers = new Vector3(
                        float.Parse(items[4], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(items[3], CultureInfo.InvariantCulture.NumberFormat),
                        float.Parse(items[2], CultureInfo.InvariantCulture.NumberFormat)
                    )
                });
            }
            
            if (_start) OnActivate();
        }
    }
    
}