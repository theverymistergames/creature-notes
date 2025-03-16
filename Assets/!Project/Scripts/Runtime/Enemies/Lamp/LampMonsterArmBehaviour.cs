using System.Threading;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Audio;
using MisterGames.Common.Easing;
using MisterGames.Common.GameObjects;
using MisterGames.Common.Maths;
using MisterGames.Common.Tick;
using MisterGames.Logic.Interactives;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Enemies.Lamp {
    
    public sealed class LampMonsterArmBehaviour : MonoBehaviour, IUpdate {
    
        [SerializeField] private LampBehaviour _lamp;

        [Header("Weight")]
        [SerializeField] [Range(0f, 1f)] private float _weight;

        [Header("Monster")]
        [SerializeField] private Transform _monsterTransform;
        [SerializeField] private GameObject[] _monsterVisuals;
        [SerializeField] private float _scaleStart = 0f;
        [SerializeField] private float _scaleEnd = 1.5f;
        [SerializeField] [Range(0f, 1f)] private float _visibleMinWeight = 0.7f;
        [SerializeField] private bool _visibleOnMaxWeight = true;
        
        [Header("Blink")]
        [SerializeField] [MinMaxSlider(0f, 1f)] private Vector2 _activateBlinkWeightRange = new(0.1f, 1f);
        [SerializeField] [Min(0f)] private float _blinkSmoothing = 7f;
        [SerializeField] private float _blinkSpeedStart = 4f;
        [SerializeField] private float _blinkSpeedEnd = 6f;
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _blinkDurationStart;
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _blinkDurationEnd;
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _blinkCooldownStart;
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _blinkCooldownEnd;
        [SerializeField] [Range(0f, 1f)] private float _blinkProbabilityStart;
        [SerializeField] [Range(0f, 1f)] private float _blinkProbabilityEnd = 1f;
        [SerializeField] [Range(0f, 1f)] private float _blinkDepthStart = 0.2f;
        [SerializeField] [Range(0f, 1f)] private float _blinkDepthEnd = 1f;
        
        [Header("Switch")]
        [SerializeField] [MinMaxSlider(0f, 1f)] private Vector2 _activateSwitchWeightRange = new(0.1f, 1f);
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _switchDurationStart;
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _switchDurationEnd;
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _switchCooldownStart;
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _switchCooldownEnd;
        [SerializeField] [Range(0f, 1f)] private float _switchProbabilityStart;
        [SerializeField] [Range(0f, 1f)] private float _switchProbabilityEnd;

        [Header("Sounds")]
        [SerializeField] [Range(0f, 2f)] private float _pitch = 1f;
        [SerializeField] [Range(0f, 2f)] private float _pitchRandom = 0.1f;
        [SerializeField] [Range(0f, 1f)] private float _spatialBlend = 0f;
        
        [Header("Blink Sounds")]
        [SerializeField] [Range(0f, 2f)] private float _blinkVolume = 1f;
        [SerializeField] [Min(0f)] private float _blinkVolumeSmoothing = 7f;
        [SerializeField] private AnimationCurve _blinkVolumeCurve = EasingType.Linear.ToAnimationCurve();
        [SerializeField] [Min(0f)] private float _blinkFadeOut = 0.3f;
        [SerializeField] private AudioClip[] _lampBlinkSounds;
        
        [Header("Switch Sounds")]
        [SerializeField] [Range(0f, 2f)] private float _switchVolume = 1f;
        [SerializeField] [Range(0f, 2f)] private float _switchVolumeRandom = 0.1f;
        [SerializeField] private AudioClip[] _lampSwitchOnSounds;
        [SerializeField] private AudioClip[] _lampSwitchOffSounds;

        private CancellationTokenSource _enableCts;
        
        private Vector3 _monsterScale;
        private bool _hasMonster;
        
        private float _switchTimer;
        private float _switchCooldown;
        private float _blinkTimer;
        private float _blinkCooldown;
        private float _blinkSmoothed = 1f;
        
        private AudioHandle _blinkSound;
        
        private void Awake() {
            _hasMonster = _monsterTransform != null;
            _monsterScale = _hasMonster ? _monsterTransform.transform.localScale : default;
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            SetWeight(_weight);
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            PlayerLoopStage.Update.Unsubscribe(this);
        }
        
        public void SetWeight(float weight) {
            _weight = Mathf.Clamp01(weight);

            if (!enabled) return;
            
            if (_weight < 1f) {
                StartFailureSoundLoop(_enableCts.Token);
                PlayerLoopStage.Update.Subscribe(this);
                return;
            }

            _blinkSmoothed = 1f;
            
            _lamp.Weight = 0f;
            
            _monsterVisuals.SetActive(_visibleOnMaxWeight);
            ProcessMonster(weight: 1f, lampWeight: 0f);
            
            PlaySwitchSound(_lampSwitchOffSounds, _enableCts.Token);
            _blinkSound.Release();
            
            PlayerLoopStage.Update.Unsubscribe(this);
        }
        
        void IUpdate.OnUpdate(float dt) {
            if (_weight >= 1f) return;
            
            float blinkWeight = GetBlinkWeight(_weight, dt);
            float switchWeight = GetSwitchWeight(_weight, dt);
            
            _blinkSmoothed = _blinkSmoothed.SmoothExpNonZero(blinkWeight, _blinkSmoothing, dt);
            float lampWeight = _blinkSmoothed * switchWeight;
            float oldLampWeight = _lamp.Weight;
            
            _lamp.Weight = lampWeight;
            ProcessMonster(_weight, switchWeight);
            ProcessBlinkSound(lampWeight, dt);
            ProcessSwitchSounds(lampWeight, oldLampWeight);
        }

        private float GetSwitchWeight(float weight, float dt) {
            _switchTimer -= dt;

            if (weight < _activateSwitchWeightRange.x || weight > _activateSwitchWeightRange.y) {
                return 1f;
            }

            if (_switchTimer > 0f) {
                return 0f;
            }

            if (_switchTimer < -_switchCooldown && 
                Mathf.Lerp(_switchProbabilityStart, _switchProbabilityEnd, weight) > Random.value) 
            {
                _switchTimer = Mathf.Lerp(_switchDurationStart.GetRandomInRange(), _switchDurationEnd.GetRandomInRange(), weight);
                _switchCooldown = Mathf.Lerp(_switchCooldownStart.GetRandomInRange(), _switchCooldownEnd.GetRandomInRange(), weight);
            }

            return 1f;
        }
        
        private float GetBlinkWeight(float weight, float dt) {
            _blinkTimer -= dt;

            if (weight < _activateBlinkWeightRange.x || weight > _activateBlinkWeightRange.y) {
                return 1f;
            }

            if (_blinkTimer > 0f) {
                float blinkDepth = Mathf.Lerp(_blinkDepthStart, _blinkDepthEnd, weight);
                float noise = Mathf.PerlinNoise1D(Time.time * Mathf.Lerp(_blinkSpeedStart, _blinkSpeedEnd, weight)) * 2f - 1;
                
                return Mathf.Clamp01(1f - blinkDepth + noise * 0.5f * blinkDepth);
            }

            if (_blinkTimer < -_blinkCooldown && 
                Mathf.Lerp(_blinkProbabilityStart, _blinkProbabilityEnd, weight) > Random.value) 
            {
                _blinkTimer = Mathf.Lerp(_blinkDurationStart.GetRandomInRange(), _blinkDurationEnd.GetRandomInRange(), weight);
                _blinkCooldown = Mathf.Lerp(_blinkCooldownStart.GetRandomInRange(), _blinkCooldownStart.GetRandomInRange(), weight);
            }

            return 1f;
        }
        
        private void ProcessMonster(float weight, float lampWeight) {
            if (!_hasMonster) return;
            
            float scaleWeight = _visibleMinWeight < 1f 
                ? Mathf.Clamp01((weight - _visibleMinWeight) / (1f - _visibleMinWeight))
                : 1f;
            
            _monsterTransform.localScale = _monsterScale * Mathf.Lerp(_scaleStart, _scaleEnd, scaleWeight);
            _monsterVisuals.SetActive(lampWeight <= 0f && weight >= _visibleMinWeight);
        }
        
        private void ProcessBlinkSound(float lampWeight, float dt) {
            float vol = _blinkVolumeCurve.Evaluate(1f - lampWeight) * _blinkVolume;
            _blinkSound.Volume = _blinkSound.Volume.SmoothExpNonZero(vol, _blinkVolumeSmoothing, dt);
        }

        private void ProcessSwitchSounds(float lampWeight, float oldLampWeight) {
            if (lampWeight > 0f && oldLampWeight <= 0f) {
                PlaySwitchSound(_lampSwitchOnSounds, _enableCts.Token);
                return;
            }
            
            if (lampWeight <= 0f && oldLampWeight > 0f) {
                PlaySwitchSound(_lampSwitchOffSounds, _enableCts.Token);
            }
        }

        private void PlaySwitchSound(AudioClip[] clips, CancellationToken cancellationToken) {
            if (clips is not { Length: > 0 }) return;
            
            AudioPool.Main.Play(
                clip: AudioPool.Main.ShuffleClips(clips),
                _lamp.transform.position,
                volume: _switchVolume + Random.Range(-_switchVolumeRandom, _switchVolumeRandom),
                fadeIn: 0f,
                fadeOut: -1f,
                _pitch + Random.Range(-_pitchRandom, _pitchRandom),
                _spatialBlend,
                normalizedTime: 0f,
                mixerGroup: null,
                AudioOptions.ApplyOcclusion | AudioOptions.AffectedByTimeScale,
                cancellationToken 
            );
        }
        
        private void StartFailureSoundLoop(CancellationToken cancellationToken) {
            if (_lampBlinkSounds is not { Length: > 0 } || _blinkSound.IsValid()) return;
            
            _blinkSound = AudioPool.Main.Play(
                clip: AudioPool.Main.ShuffleClips(_lampBlinkSounds),
                _lamp.transform.position,
                volume: 0f,
                fadeIn: 0f,
                fadeOut: _blinkFadeOut,
                _pitch + Random.Range(-_pitchRandom, _pitchRandom),
                _spatialBlend,
                normalizedTime: Random.value,
                mixerGroup: null,
                AudioOptions.ApplyOcclusion | AudioOptions.Loop | AudioOptions.AffectedByTimeScale,
                cancellationToken 
            );
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (!Application.isPlaying || _enableCts == null) return;
            
            SetWeight(_weight);
        }
#endif
    }
    
}