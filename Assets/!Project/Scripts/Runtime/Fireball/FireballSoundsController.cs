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

namespace _Project.Scripts.Runtime.Fireball {
    
    public sealed class FireballSoundsController : MonoBehaviour, IActorComponent, IUpdate {

        [Header("Settings")]
        [SerializeField] private Vector3 _localPosition;
        
        [Header("Fire Fail")]
        [SerializeField] private OneShotSound _fireFailSound;
        
        [Header("Stages")]
        [SerializeField] private StageSound[] _stageSounds;
        
        [Serializable]
        private struct StageSound {
            public FireballShootingBehaviour.Stage stage;
            public LoopSound[] loopSounds;
            public OneShotSound[] oneShotSounds;
        }

        [Serializable]
        private struct LoopSound {
            [MinMaxSlider(0f, 1f)] public Vector2 progressRange;
            [MinMaxSlider(0f, 1f)] public Vector2 startTime;
            [Range(0f, 1f)] public float startVolume;
            [Range(0f, 1f)] public float endVolume;
            public AnimationCurve volumeCurve;
            [MinMaxSlider(0f, 2f)] public Vector2 pitch;
            [Range(0f, 1f)] public float spatialBlend;
            [Min(0f)] public float fadeIn;
            [Min(0f)] public float fadeOut;
            public AudioClip[] clipVariants;
        }
        
        [Serializable]
        private struct OneShotSound {
            public float volume;
            [MinMaxSlider(0f, 2f)] public Vector2 pitch;
            [Range(0f, 1f)] public float spatialBlend;
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
        
        private CancellationTokenSource _enableCts;
        private FireballShootingBehaviour _fireballShootingBehaviour;
        private FireballShaderController _fireballShaderController;
        private Transform _transform;
        private StageSound _currentStageSound;
        
        void IActorComponent.OnAwake(IActor actor) {
            _transform = actor.Transform;
            _fireballShootingBehaviour = actor.GetComponent<FireballShootingBehaviour>();
            _fireballShaderController = actor.GetComponent<FireballShaderController>();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            PlayerLoopStage.Update.Subscribe(this);
            
            _fireballShootingBehaviour.OnStageChanged += OnStageChanged;
            _fireballShootingBehaviour.OnCannotCharge += OnCannotCharge;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            PlayerLoopStage.Update.Unsubscribe(this);
            
            _fireballShootingBehaviour.OnStageChanged -= OnStageChanged;
            _fireballShootingBehaviour.OnCannotCharge -= OnCannotCharge;
        }

        private void OnStageChanged(FireballShootingBehaviour.Stage previous, FireballShootingBehaviour.Stage current) {
            MoveLastStageLoopSoundsToFadeOutList();
            SelectCurrentStageSound(current);
            PlayCurrentStageLoopSounds();
            PlayCurrentStageOneShotSounds();
        }

        private void OnCannotCharge(FireballShootingBehaviour.Stage stage) {
            if (stage != FireballShootingBehaviour.Stage.Cooldown) return;
            
            PlayOneShotSound(ref _fireFailSound);
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
                _localPosition,
                sound.volume,
                sound.pitch.GetRandomInRange(),
                sound.spatialBlend,
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
                    _localPosition,
                    volume: 0f,
                    sound.pitch.GetRandomInRange(),
                    sound.spatialBlend,
                    sound.startTime.GetRandomInRange(),
                    loop: true,
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
            ProcessCurrentStageLoopSounds();
            ProcessFadeOutLoopSounds();
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