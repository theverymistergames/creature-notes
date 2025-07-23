using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Common.Async;
using MisterGames.Common.Audio;
using MisterGames.Common.Easing;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Enemies.Painting {
    
    public sealed class PaintingMonsterAttack : MonoBehaviour, IActorComponent {

        [Header("Scream")]
        [SerializeField] private AudioClip[] _screamSounds;
        [SerializeField] [Range(0f, 2f)] private float _volume = 1f;
        [SerializeField] [Min(0f)] private float _fadeIn = 0.25f;
        [SerializeField] [Min(0f)] private float _fadeOut = 0.25f;
        [SerializeField] [Range(0f, 2f)] private float _pitch = 1f;
        [SerializeField] [Range(0f, 2f)] private float _pitchRandom = 0.1f;
        [SerializeField] [Range(0f, 1f)] private float _spatialBlend = 1f;
        [SerializeField] private AudioMixerGroup _mixerGroup;
        
        [Header("Stun")]
        [SerializeField] private AudioClip _stunSound;
        [SerializeField] [Min(0f)] private float _stunDelay;
        [SerializeField] [Range(0f, 2f)] private float _stunVolume = 1f;
        [SerializeField] [Range(0f, 2f)] private float _stunPitch = 1f;
        [SerializeField] [Range(0f, 2f)] private float _stunPitchRandom = 0.1f;
        [SerializeField] [Range(0f, 1f)] private float _stunSpatialBlend = 0f;
        [SerializeField] [Min(0f)] private float _stunFadeIn;
        [SerializeField] [Min(0f)] private float _stunFadeOut;
        [SerializeField] private AnimationCurve _stunFadeInCurve = EasingType.Linear.ToAnimationCurve();
        [SerializeField] private AnimationCurve _stunFadeOutCurve = EasingType.Linear.ToAnimationCurve();
        [SerializeField] private AudioMixerGroup _stunMixerGroup;
        
        [Header("Stun Influence")]
        [SerializeField] private AudioMixer _audioMixer;
        [Space]
        [SerializeField] private string _mainVolume = "MainVolume";
        [SerializeField] [Range(-80f, 20f)] private float _mainVolumeEnd = -20f;
        [SerializeField] private AnimationCurve _mainVolumeFadeDownCurve = EasingType.Linear.ToAnimationCurve();
        [SerializeField] private AnimationCurve _mainVolumeFadeUpCurve = EasingType.Linear.ToAnimationCurve();
        [Space]
        [SerializeField] private string _mainLowPassFreq = "MainLPFreq";
        [SerializeField] [Range(10f, 22000f)] private float _mainLowPassFreqEnd = 500f;
        [SerializeField] private AnimationCurve _mainLowPassFreqFadeDownCurve = EasingType.Linear.ToAnimationCurve();
        [SerializeField] private AnimationCurve _mainLowPassFreqFadeUpCurve = EasingType.Linear.ToAnimationCurve();

        private CancellationTokenSource _enableCts;
        private CancellationTokenSource _attackCts;
        private Monster _monster;
        
        void IActorComponent.OnAwake(IActor actor) {
            _monster = actor.GetComponent<Monster>();
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            
            _monster.OnMonsterEvent += OnMonsterEvent;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            
            _monster.OnMonsterEvent -= OnMonsterEvent;
            
            StopAttack();
            
            _audioMixer.SetFloat(_mainVolume, 0f);
            _audioMixer.SetFloat(_mainLowPassFreq, 22000f);
        }

        private void OnMonsterEvent(MonsterEventType evt) {
            switch (evt) {
                case MonsterEventType.AttackStart:
                    AsyncExt.RecreateCts(ref _attackCts);
                    StartAttack(_attackCts.Token, _enableCts.Token).Forget();
                    break;
                
                case MonsterEventType.AttackFinish:
                case MonsterEventType.Death:
                case MonsterEventType.Reset:
                    StopAttack();
                    break;
            }
        }

        private async UniTask StartAttack(CancellationToken attackToken, CancellationToken stunToken) {
            var scream = AudioPool.Main.Play(
                AudioPool.Main.ShuffleClips(_screamSounds),
                _monster.transform.position,
                _volume,
                _fadeIn,
                _fadeOut,
                _pitch + Random.Range(-_pitchRandom, _pitchRandom),
                _spatialBlend,
                normalizedTime: 0f,
                _mixerGroup,
                AudioOptions.ApplyOcclusion | AudioOptions.AffectedByTimeScale | AudioOptions.AffectedByVolumes,
                attackToken
            );

            await UniTask.Delay(TimeSpan.FromSeconds(_stunDelay), cancellationToken: attackToken)
                .SuppressCancellationThrow();
            
            if (attackToken.IsCancellationRequested) return;
            
            var stun = AudioPool.Main.Play(
                _stunSound,
                _monster.transform.position,
                volume: 0f,
                fadeIn: 0f,
                _stunFadeOut,
                _stunPitch + Random.Range(-_stunPitchRandom, _stunPitchRandom),
                _stunSpatialBlend,
                normalizedTime: 0f,
                _stunMixerGroup,
                AudioOptions.None,
                stunToken
            );

            float t = 0f;
            float speed = _stunFadeIn > 0f ? 1f / _stunFadeIn : float.MaxValue;
            
            float startMainVolume = _audioMixer.GetFloat(_mainVolume, out float vol) ? vol : 0f;
            float startMainLowPassFreq = _audioMixer.GetFloat(_mainLowPassFreq, out float freq) ? freq : 22000f;
            
            while (!attackToken.IsCancellationRequested) {
                t = Mathf.Clamp01(t + Time.deltaTime * speed);
                
                stun.Volume = Mathf.Lerp(0f, _stunVolume, _stunFadeInCurve.Evaluate(t));
                
                _audioMixer.SetFloat(_mainVolume, Mathf.Lerp(startMainVolume, _mainVolumeEnd, _mainVolumeFadeDownCurve.Evaluate(t)));
                _audioMixer.SetFloat(_mainLowPassFreq, Mathf.Lerp(startMainLowPassFreq, _mainLowPassFreqEnd, _mainLowPassFreqFadeDownCurve.Evaluate(t)));
                
                await UniTask.Yield();
            }
            
            t = 0f;
            speed = _stunFadeOut > 0f ? 1f / _stunFadeOut : float.MaxValue;

            float startVolume = stun.Volume;
            startMainVolume = _audioMixer.GetFloat(_mainVolume, out vol) ? vol : 0f;
            startMainLowPassFreq = _audioMixer.GetFloat(_mainLowPassFreq, out freq) ? freq : 22000f;
            
            while (!stunToken.IsCancellationRequested && t < 1f) {
                t = Mathf.Clamp01(t + Time.deltaTime * speed);
                
                stun.Volume = Mathf.Lerp(startVolume, 0f, _stunFadeOutCurve.Evaluate(t));
                
                _audioMixer.SetFloat(_mainVolume, Mathf.Lerp(startMainVolume, 0f, _mainVolumeFadeUpCurve.Evaluate(t)));
                _audioMixer.SetFloat(_mainLowPassFreq, Mathf.Lerp(startMainLowPassFreq, 22000f, _mainLowPassFreqFadeUpCurve.Evaluate(t)));
                
                await UniTask.Yield();
            }
            
            stun.Release();
        }
        
        private void StopAttack() {
            AsyncExt.DisposeCts(ref _attackCts);
        }
    }
    
}