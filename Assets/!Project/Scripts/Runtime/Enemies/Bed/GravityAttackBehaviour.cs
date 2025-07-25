﻿using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Character.Core;
using MisterGames.Character.Motion;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Easing;
using MisterGames.Common.Lists;
using MisterGames.Common.Maths;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Enemies.Bed {
    
    public sealed class GravityAttackBehaviour : MonoBehaviour, IActorComponent {

        [SerializeField] private Transform _gravitySource;

        [Header("Flip")]
        [SerializeField] [MinMaxSlider(0f, 90f)] private Vector2 _flipAngle; 
        [SerializeField] private Vector2 _flipMagnitude = Vector2.one;
        [SerializeField] [Min(0f)] private float _flipDuration = 0.2f;
        [SerializeField] private bool _blockCharacterGravityAlign = true;
        
        [Header("Settle")]
        [SerializeField] [Min(0f)] private float _settleMagnitudeStart = 0.001f;
        [SerializeField] [Min(0f)] private float _settleMagnitudeEnd = 0.001f;
        [SerializeField] [Min(0f)] private float _settleDuration = 0.1f;
        
        [Header("Rotation")]
        [SerializeField] [Min(0f)] private float _rotationMagnitudeStart = 0.001f;
        [SerializeField] [Min(0f)] private float _rotationMagnitudeEnd = 0.001f;
        [SerializeField] [Min(0f)] private float _rotationSpeedStart = 1f;
        [SerializeField] [Min(0f)] private float _rotationSpeedEnd = 1f;
        [SerializeField] [Min(0f)] private float _rotationDuration = 3f;
        [SerializeField] private AnimationCurve _rotationCurve = EasingType.Linear.ToAnimationCurve();

        [Header("Stop")]
        [SerializeField] private StopMode _stopMode;
        [SerializeField] private Vector2 _stopMagnitude = Vector2.one;
        [SerializeField] private bool _shuffleDirections;
        [SerializeField] private Vector3[] _randomStopDirections;

        [Header("Difficulty")]
        [SerializeField] private DifficultyData[] _difficultyData;
        
        [Serializable]
        private struct DifficultyData {
            [Min(0)] public int difficultyLevel;
            public StopMode stopMode;
            public Vector3[] randomStopDirections;
        }
        
        private enum StopMode {
            ReturnToNormalGravity,
            UseLastRotation,
            RandomOnUnitSphere,
            RandomFromList
        }

        public bool IsAttackInProcess { get; private set; }

        private CancellationTokenSource _enableCts;
        private Monster _monster;
        private byte _operationId;
        private int _lastStopDirectionIndex = -1;

        void IActorComponent.OnAwake(IActor actor) {
            _monster = actor.GetComponent<Monster>();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            ReturnToNormalGravity();
        }
        
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        public void StartAntiGravity() {
            StartAntiGravityAsync(_enableCts.Token).Forget();
        }

        [Button(mode: ButtonAttribute.Mode.Runtime)]
        public void StopAntiGravity() {
            _operationId++;
            IsAttackInProcess = false;
            
            UnblockCharacterGravityAlign();

            float stopMagnitude = _stopMagnitude.GetRandomInRange();
            
            GetStopData(out var stopMode, out var stopDirections);
            
            switch (stopMode) {
                case StopMode.ReturnToNormalGravity:
                    _gravitySource.forward = Vector3.down;
                    _gravitySource.localScale = Vector3.one;
                    break;
                
                case StopMode.UseLastRotation:
                    _gravitySource.localScale = Vector3.one.WithZ(stopMagnitude);
                    break;
                
                case StopMode.RandomOnUnitSphere:
                    _gravitySource.forward = Random.onUnitSphere;
                    _gravitySource.localScale = Vector3.one.WithZ(stopMagnitude);
                    break;
                
                case StopMode.RandomFromList:
                    int excludeIndex = _shuffleDirections ? _lastStopDirectionIndex : -1;
                    int index = stopDirections.GetRandomIndex(excludeIndex);
                    _lastStopDirectionIndex = index;

                    if (index >= 0) {
                        _gravitySource.forward = stopDirections[index].normalized;
                        _gravitySource.localScale = Vector3.one.WithZ(stopMagnitude);
                    }
                    else {
                        _gravitySource.forward = Vector3.down;
                        _gravitySource.localScale = Vector3.one;
                    }
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        [Button(mode: ButtonAttribute.Mode.Runtime)]
        public void ReturnToNormalGravity() {
            _operationId++;
            IsAttackInProcess = false;
            
            UnblockCharacterGravityAlign();
            
            _gravitySource.forward = Vector3.down;
            _gravitySource.localScale = Vector3.one;
            
            _lastStopDirectionIndex = -1;
        }

        private async UniTask StartAntiGravityAsync(CancellationToken cancellationToken) {
            byte id = ++_operationId;
            
            IsAttackInProcess = true;
            var originDir = _gravitySource.forward;

            var flatDir = RandomExtensions.OnUnitCircle(-originDir);
            var axis = Vector3.Cross(flatDir, -originDir);
            var flipDir = Quaternion.AngleAxis(_flipAngle.GetRandomInRange(), axis) * flatDir;
            
            if (_flipDuration > 0f) {
                if (_blockCharacterGravityAlign) BlockCharacterGravityAlign(cancellationToken);

                await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
                
                if (cancellationToken.IsCancellationRequested || _operationId != id) return;

                _gravitySource.forward = flipDir;
                _gravitySource.localScale = Vector3.one.WithZ(_flipMagnitude.GetRandomInRange());
            
                await UniTask.Delay(TimeSpan.FromSeconds(_flipDuration), cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();
            
                if (cancellationToken.IsCancellationRequested || _operationId != id) return;
            }

            _gravitySource.forward = -flipDir;

            var startRot = _gravitySource.rotation;
            var endRot = _gravitySource.rotation * 
                         Quaternion.FromToRotation(-flipDir, RandomExtensions.OnUnitCircle(flipDir));
            
            float t = 0f;
            float inc = _rotationDuration > 0f ? 1f / _rotationDuration : float.MaxValue;
            float tSettle = _rotationDuration > 0f ? _settleDuration / _rotationDuration : 0f;
            float tRot = 0f;
            
            bool unblockedAlign = false;

            while (!cancellationToken.IsCancellationRequested && _operationId == id && t < 1f) {
                float dt = Time.fixedDeltaTime;
                t += dt * inc;
                
                float speed = Mathf.Lerp(_rotationSpeedStart, _rotationSpeedEnd, _rotationCurve.Evaluate(t));
                tRot += dt * speed;

                float magnitude = t < tSettle
                    ? Mathf.Lerp(_settleMagnitudeStart, _settleMagnitudeEnd, t / tSettle)
                    : Mathf.Lerp(_rotationMagnitudeStart, _rotationMagnitudeEnd, t);

                _gravitySource.rotation = Quaternion.SlerpUnclamped(startRot, endRot, tRot);
                _gravitySource.localScale = Vector3.one.WithZ(magnitude);
                
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate);

                if (!unblockedAlign) {
                    if (cancellationToken.IsCancellationRequested || _operationId != id) return;
                    
                    unblockedAlign = true;
                    UnblockCharacterGravityAlign();
                }
            }
        }
        
        private void GetStopData(out StopMode stopMode, out Vector3[] stopDirections) {
            int difficulty = _monster.Difficulty;

            for (int i = 0; i < _difficultyData.Length; i++) {
                ref var data = ref _difficultyData[i];
                if (data.difficultyLevel != difficulty) continue;
                
                stopMode = data.stopMode;
                stopDirections = data.randomStopDirections;
                return;
            }

            stopMode = _stopMode;
            stopDirections = _randomStopDirections;
        }

        private void BlockCharacterGravityAlign(CancellationToken cancellationToken) {
            if (CharacterSystem.Instance.GetCharacter()?.TryGetComponent(out CharacterGravity characterGravity) ?? false) {
                characterGravity.BlockGravityAlign(this, block: true, cancellationToken);
            }
        }
        
        private void UnblockCharacterGravityAlign() {
            if (CharacterSystem.Instance.GetCharacter()?.TryGetComponent(out CharacterGravity characterGravity) ?? false) {
                characterGravity.BlockGravityAlign(this, block: false);
            }
        }
    }
    
}