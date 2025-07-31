using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MisterGames.Character.Core;
using MisterGames.Character.View;
using MisterGames.Input.Actions;
using UnityEngine;

namespace _Project.Scripts.Runtime.Forest {
    public class CameraRecorder : MonoBehaviour {

        [SerializeField] private List<Data> _dataArray;
        [SerializeField] private InputActionKey _recordInput;
        [SerializeField] private InputActionKey _playInput;
        [SerializeField] private InputActionKey _clearInput;
        [SerializeField] private bool _isRecording;
        
        [Serializable]
        private struct Data {
            public Vector3 pos;
            public Quaternion rot;
            public float time;
        }
            
        private CharacterViewPipeline _viewPipeline;
        private bool _startedRecord;
        private bool _isPlaying;
        private float _time;
        private byte _playId;
        
        private void Awake() {
            _viewPipeline = CharacterSystem.Instance.GetCharacter().GetComponent<CharacterViewPipeline>();
        }

        private void OnEnable() {
            _recordInput.OnPress += RecordInputOnOnPress;
            _playInput.OnPress += PlayInputOnOnPress;
            _clearInput.OnPress += ClearRecord;
        }
        private void OnDisable() {
            _recordInput.OnPress -= RecordInputOnOnPress;
            _playInput.OnPress -= PlayInputOnOnPress;
            _clearInput.OnPress -= ClearRecord;
            
            _playId++;
        }

        private void ClearRecord() {
            _dataArray ??= new List<Data>();
            _dataArray.Clear();
        }
        
        private async void PlayInputOnOnPress() {
            if (_isRecording) return;

            byte id = ++_playId;
            
            if (_isPlaying) {
                _isPlaying = false;
                Debug.Log("stop play");
                return;
            }

            if (_dataArray.Count <= 0) return;
            
            Debug.Log("start play");
            
            _isPlaying = true;
            float time = 0f;
            float lastTime = _dataArray[^1].time;

            while (id == _playId && time <= lastTime) {
                time += Time.deltaTime;

                var data = GetData(time);
                    
                _viewPipeline.HeadPosition = data.pos;
                _viewPipeline.HeadRotation = data.rot;
                
                await UniTask.Yield();
            }
            
            Debug.Log("finish play");
        }

        private Data GetData(float time) {
            if (time < _dataArray[0].time) {
                return _dataArray[0];
            }
            
            for (var i = 0; i < _dataArray.Count; i++) {
                var data = _dataArray[i];
                
                if (time > data.time) continue;
                
                if (i >= _dataArray.Count - 1) {
                    return data;
                }

                var next = _dataArray[i + 1];
                float t = (time - data.time) / (next.time - data.time);
                
                return new Data {
                    pos = Vector3.Lerp(data.pos, next.pos, t), 
                    rot = Quaternion.Slerp(data.rot, next.rot, t),
                    time = time
                };
            }

            return _dataArray[^1];
        }
        
        private void RecordInputOnOnPress() {
            ToggleRecord();
        }
        
        private void LateUpdate() {
            if (!_isRecording) return;

            float dt = Time.deltaTime;
            _time += dt;
            
            _dataArray.Add(new Data { pos = _viewPipeline.HeadPosition, rot = _viewPipeline.HeadRotation, time = _time });
        }

        private void ToggleRecord() {
            if (_isRecording) {
                _isRecording = false;
                Debug.Log("stop record");
                return;
            }
            Debug.Log("start record");
            _isRecording = true;

            if (!_startedRecord) {
                _startedRecord = true;

                _dataArray ??= new List<Data>();
                _dataArray?.Clear();
            }
        }
    }
    
}