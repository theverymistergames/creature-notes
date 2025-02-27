using System;
using System.Collections.Generic;
using System.Threading;
using MisterGames.Actors;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Audio;
using MisterGames.Common.Maths;
using MisterGames.Common.Tick;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Fireball {
    
    public sealed class FireballSoundsController : MonoBehaviour, IActorComponent, IUpdate {

        [Header("Moving Fire")]
        [SerializeField] private float _stereoPanMul = 1f;
        [SerializeField] [Min(0f)] private float _centerOffsetMax = 0f;
        [SerializeField] [Min(0f)] private float _centerOffsetVolumeMul = 1f;
        [SerializeField] [Min(0f)] private float _volumeSmoothing = 10f;
        [SerializeField] [Range(0f, 1f)] private float _movingFireSpatialBlend = 1f;
        [SerializeField] private AudioClip _movingFireSound;
        [SerializeField] private MovingFireStageSound[] _movingFireStageSounds;

        [Header("Stages")]
        [SerializeField] [Range(0f, 1f)] private float _stageSpatialBlend = 0f;
        [SerializeField] private OneShotSound _fireFailSound;
        [SerializeField] private StageSound[] _stageSounds;

        [Serializable]
        private struct MovingFireStageSound {
            public FireballShootingBehaviour.Stage stage;
            [Range(0f, 1f)] public float startVolume;
            [Range(0f, 1f)] public float endVolume;
        }
        
        [Serializable]
        private struct StageSound {
            public FireballShootingBehaviour.Stage stage;
            public OneShotSound[] oneShotSounds;
            public LoopSound[] loopSounds;
        }

        [Serializable]
        private struct LoopSound {
            [MinMaxSlider(0f, 1f)] public Vector2 progressRange;
            [MinMaxSlider(0f, 1f)] public Vector2 startTime;
            [Range(0f, 1f)] public float startVolume;
            [Range(0f, 1f)] public float endVolume;
            public AnimationCurve volumeCurve;
            [MinMaxSlider(0f, 2f)] public Vector2 pitch;
            [Min(0f)] public float fadeIn;
            [Min(0f)] public float fadeOut;
            public AudioClip[] clipVariants;
        }
        
        [Serializable]
        private struct OneShotSound {
            public float volume;
            [MinMaxSlider(0f, 2f)] public Vector2 pitch;
            public AudioClip[] clipVariants;
        }

        private readonly struct LoopSoundData {
            
            public readonly AudioHandle audioHandle;
            public readonly Vector2 progressRange;
            public readonly Vector2 volumeOverProgress;
            public readonly AnimationCurve volumeCurve;
            public readonly float fadeIn;
            public readonly float fadeOut;
            
            public LoopSoundData(AudioHandle audioHandle, Vector2 progressRange, Vector2 volumeOverProgress, AnimationCurve volumeCurve, float fadeIn, float fadeOut) {
                this.audioHandle = audioHandle;
                this.progressRange = progressRange;
                this.volumeOverProgress = volumeOverProgress;
                this.volumeCurve = volumeCurve;
                this.fadeIn = fadeIn;
                this.fadeOut = fadeOut;
            }
        }

        private readonly struct FadeOutData {
            
            public readonly AudioHandle audioHandle;
            public readonly float startTime;
            public readonly float startVolume;
            public readonly float fadeOut;
            
            public FadeOutData(AudioHandle audioHandle, float startTime, float startVolume, float fadeOut) {
                this.audioHandle = audioHandle;
                this.startVolume = startVolume;
                this.fadeOut = fadeOut;
                this.startTime = startTime;
            }
        }

        private readonly List<LoopSoundData> _currentStageLoopSounds = new();
        private readonly List<FadeOutData> _fadeOutList = new();
        private AudioHandle _movingFireAudioHandle;
        private MovingFireStageSound _currentMovingFireStageSound;
        private StageSound _currentStageSound;
        
        private CancellationTokenSource _enableCts;
        private FireballShootingBehaviour _fireballShootingBehaviour;
        private FireballShaderController _fireballShaderController;
        private FireballShootingData _fireballShootingData;
        private Transform _transform;
        
        void IActorComponent.OnAwake(IActor actor) {
            _transform = actor.Transform;
            _fireballShootingBehaviour = actor.GetComponent<FireballShootingBehaviour>();
            _fireballShaderController = actor.GetComponent<FireballShaderController>();
        }

        void IActorComponent.OnSetData(IActor actor) {
            _fireballShootingData = actor.GetData<FireballShootingData>();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            PlayerLoopStage.Update.Subscribe(this);
            
            _fireballShootingBehaviour.OnStageChanged += OnStageChanged;
            _fireballShootingBehaviour.OnCannotCharge += OnCannotCharge;
            
            StartMovingFireSound();
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            PlayerLoopStage.Update.Unsubscribe(this);
            
            _fireballShootingBehaviour.OnStageChanged -= OnStageChanged;
            _fireballShootingBehaviour.OnCannotCharge -= OnCannotCharge;
        }

        private void StartMovingFireSound() {
            _movingFireAudioHandle = AudioPool.Main.Play(
                _movingFireSound,
                _transform,
                localPosition: default,
                attachId: 0,
                volume: 0f,
                fadeIn: 0f,
                fadeOut: -1f,
                pitch: 1f,
                _movingFireSpatialBlend,
                normalizedTime: Random.value,
                options: AudioOptions.Loop | AudioOptions.AffectedByTimeScale,
                cancellationToken: _enableCts.Token
            );
        }

        private void OnStageChanged(FireballShootingBehaviour.Stage previous, FireballShootingBehaviour.Stage current) {
            SelectCurrentMovingFireStageSound(current);
            SelectCurrentStageSound(current);
            
            MoveLastStageLoopSoundsToFadeOutList();
            PlayCurrentStageLoopSounds();
            PlayCurrentStageOneShotSounds();
        }

        private void OnCannotCharge(FireballShootingBehaviour.Stage stage) {
            if (stage != FireballShootingBehaviour.Stage.Cooldown) return;
            
            PlayOneShotSound(ref _fireFailSound);
        }

        private void SelectCurrentMovingFireStageSound(FireballShootingBehaviour.Stage stage) {
            for (int i = 0; i < _movingFireStageSounds.Length; i++) {
                ref var stageSound = ref _movingFireStageSounds[i];
                if (stageSound.stage != stage) continue;

                _currentMovingFireStageSound = stageSound;
                return;
            }

            _currentMovingFireStageSound = default;
        }
        
        private void SelectCurrentStageSound(FireballShootingBehaviour.Stage stage) {
            for (int i = 0; i < _stageSounds.Length; i++) {
                ref var stageSound = ref _stageSounds[i];
                if (stageSound.stage != stage) continue;

                _currentStageSound = stageSound;
                return;
            }

            _currentStageSound = default;
        }
        
        private void PlayCurrentStageOneShotSounds() {
            for (int j = 0; j < _currentStageSound.oneShotSounds?.Length; j++) {
                PlayOneShotSound(ref _currentStageSound.oneShotSounds[j]);
            }
        }

        private void PlayOneShotSound(ref OneShotSound sound) {
            if (sound.clipVariants is not { Length: > 0 }) return;
                    
            var clip = AudioPool.Main.ShuffleClips(sound.clipVariants);
            AudioPool.Main.Play(
                clip,
                _transform,
                localPosition: default,
                attachId: 0,
                sound.volume,
                fadeIn: 0f,
                fadeOut: -1f,
                sound.pitch.GetRandomInRange(),
                _stageSpatialBlend,
                cancellationToken: _enableCts.Token
            );
        }

        private void MoveLastStageLoopSoundsToFadeOutList() {
            float time = Time.time;
            
            for (int i = 0; i < _currentStageLoopSounds.Count; i++) {
                var sound = _currentStageLoopSounds[i];
                _fadeOutList.Add(new FadeOutData(sound.audioHandle, time, sound.audioHandle.Volume, sound.fadeOut));
            }

            _currentStageLoopSounds.Clear();
        }

        private void PlayCurrentStageLoopSounds() {
            for (int j = 0; j < _currentStageSound.loopSounds?.Length; j++) {
                ref var sound = ref _currentStageSound.loopSounds[j];
                if (sound.clipVariants is not { Length: > 0 }) continue;
                    
                var clip = AudioPool.Main.ShuffleClips(sound.clipVariants);
                var audioHandle = AudioPool.Main.Play(
                    clip,
                    _transform,
                    localPosition: default,
                    attachId: 0,
                    volume: 0f,
                    fadeIn: 0f,
                    fadeOut: -1f,
                    sound.pitch.GetRandomInRange(),
                    _stageSpatialBlend,
                    sound.startTime.GetRandomInRange(),
                    mixerGroup: default,
                    options: AudioOptions.Loop | AudioOptions.AffectedByTimeScale,
                    _enableCts.Token
                );
                
                _currentStageLoopSounds.Add(new LoopSoundData(
                    audioHandle,
                    sound.progressRange,
                    volumeOverProgress: new Vector2(sound.startVolume, sound.endVolume), 
                    sound.volumeCurve,
                    sound.fadeIn, 
                    sound.fadeOut
                ));
            }
        }
        
        void IUpdate.OnUpdate(float dt) {
            ProcessMovingFireSound(dt);
            ProcessCurrentStageLoopSounds();
            ProcessFadeOutLoopSounds();
        }

        private void ProcessMovingFireSound(float dt) {
            float stageVolume = Mathf.Lerp(_currentMovingFireStageSound.startVolume, _currentMovingFireStageSound.endVolume, _fireballShootingBehaviour.StageProgress);
            
            var centerOffset = _fireballShaderController.CenterOffset;
            float centerOffsetProgress = _centerOffsetMax > 0f ? centerOffset.magnitude / _centerOffsetMax : 0f;
            float centerOffsetProgressX = Mathf.Clamp(_centerOffsetMax > 0f ? centerOffset.x / _centerOffsetMax : 0f, -1f, 1f);
            
            float centerOffsetVolumeMul = Mathf.Lerp(0f, _centerOffsetVolumeMul, centerOffsetProgress);
            float targetVolume = stageVolume * centerOffsetVolumeMul;
            
            _movingFireAudioHandle.Volume = _movingFireAudioHandle.Volume.SmoothExpNonZero(targetVolume, _volumeSmoothing, dt);
            _movingFireAudioHandle.StereoPan = centerOffsetProgressX * _stereoPanMul;
        }
        
        private void ProcessCurrentStageLoopSounds() {
            float stageProgress = _fireballShootingBehaviour.StageProgress;
            float stageDuration = _fireballShootingBehaviour.StageDuration;
            float stageTime = stageDuration * stageProgress;
            float time = Time.time;
            
            for (int i = _currentStageLoopSounds.Count - 1; i >= 0; i--) {
                var sound = _currentStageLoopSounds[i];

                if (stageProgress > sound.progressRange.y) {
                    _currentStageLoopSounds.RemoveAt(i);
                    _fadeOutList.Add(new FadeOutData(sound.audioHandle, time, sound.audioHandle.Volume, sound.fadeOut));
                    continue;
                }
                
                float localProgress = (sound.progressRange.y - sound.progressRange.x).IsNearlyZero()
                    ? stageProgress < sound.progressRange.x ? 0f : 1f
                    : (stageProgress - sound.progressRange.x) / (sound.progressRange.y - sound.progressRange.x);
                
                float fadeInProgress = sound.fadeIn > 0f 
                    ? Mathf.Clamp01((stageTime - stageDuration * sound.progressRange.x) / sound.fadeIn)
                    : stageTime < stageDuration * sound.progressRange.x ? 0f : 1f;
                
                float volume = Mathf.Lerp(sound.volumeOverProgress.x, sound.volumeOverProgress.y, sound.volumeCurve.Evaluate(localProgress)) * 
                               fadeInProgress;

                sound.audioHandle.Volume = volume;
            }
        }

        private void ProcessFadeOutLoopSounds() {
            float time = Time.time;

            for (int i = _fadeOutList.Count - 1; i >= 0; i--) {
                var data = _fadeOutList[i];
                
                float progress = data.fadeOut > 0f ? (time - data.startTime) / data.fadeOut : 1f;
                data.audioHandle.Volume = Mathf.Lerp(data.startVolume, 0f, progress);
                
                if (progress < 1f) continue;

                _fadeOutList.RemoveAt(i);
                data.audioHandle.Release();
            }
        }
    }
    
}