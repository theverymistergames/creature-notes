using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Attributes;
using MisterGames.Common.Data;
using MisterGames.Common.Easing;
using MisterGames.Scenario.Events;
using MisterGames.Scenes.Core;
using MisterGames.Scenes.Loading;
using UnityEngine;

namespace _Project.Scripts.Runtime.Levels {
    
    public sealed class LevelService : MonoBehaviour, ILevelService {
        
        [Header("Scenes")]
        [SerializeField] private SceneReference _levelsScene;
        [SerializeField] private SceneReference _gameplayScene;
        [SerializeField] private SceneReference _menuScene;
        
        [Header("Levels")]
        [SerializeField] private EventReference _levelCounterEvent;
        [SerializeField] [Min(0f)] private float _minDurationLoading = 1f;
        
        [Header("Fader")]
        [SerializeField] [Min(-1f)] private float _fadeInToLoading = -1f;
        [SerializeField] [Min(-1f)] private float _fadeOutToLoading = -1f;
        [SerializeField] [Min(-1f)] private float _fadeInToScene = -1f;
        [SerializeField] [Min(-1f)] private float _fadeOutToScene = -1f;
        [SerializeField] private Optional<AnimationCurve> _fadeInCurve = Optional<AnimationCurve>.WithDisabled(EasingType.Linear.ToAnimationCurve());
        [SerializeField] private Optional<AnimationCurve> _fadeOutCurve = Optional<AnimationCurve>.WithDisabled(EasingType.Linear.ToAnimationCurve());
        
        public static ILevelService Instance { get; private set; }

        public event Action<int> OnLevelRequested = delegate { }; 
        public int CurrentLevel {
            get => _levelCounterEvent.GetCount();
            set => _levelCounterEvent.SetCount(value);
        }

        private CancellationToken _destroyToken;
        private byte _loadId;
        
        private void Awake() {
            Instance = this;
            _destroyToken = destroyCancellationToken;
        }

        private void OnDestroy() {
            Instance = null;
        }

        public UniTask LoadLastSavedLevel(float fadeIn = -1f, float fadeOut = -1f) {
            return LoadLevel(0, fadeIn, fadeOut);
        }

        public async UniTask LoadLevel(int level, float fadeIn = -1f, float fadeOut = -1f) {
            if (fadeIn < 0f) fadeIn = _fadeInToLoading;
            if (fadeOut < 0f) fadeOut = _fadeOutToScene;
            
            await Fader.Main.FadeInAsync(fadeIn, _fadeInCurve.GetOrDefault());
            if (_destroyToken.IsCancellationRequested) return;

            _levelCounterEvent.SetCount(level);
            OnLevelRequested.Invoke(level);
            
            await SceneLoader.UnloadSceneAsync(_menuScene.scene);
            if (_destroyToken.IsCancellationRequested) return;
            
            LoadingService.Instance.ShowLoadingScreen(true);
            
            float fadeOutFinal = _fadeOutToLoading;

            if (!SceneLoader.IsSceneLoaded(_gameplayScene.scene)) {
                await SceneLoader.LoadSceneAsync(_gameplayScene.scene, makeActive: false);
                if (_destroyToken.IsCancellationRequested) return;
            }

            if (!SceneLoader.IsSceneLoaded(_levelsScene.scene)) {
                fadeOutFinal = fadeOut;
                
                await Fader.Main.FadeOutAsync(_fadeOutToLoading, _fadeOutCurve.GetOrDefault());
                if (_destroyToken.IsCancellationRequested) return;

                float startLoadTime = Time.realtimeSinceStartup;
            
                await SceneLoader.LoadSceneAsync(_levelsScene.scene, makeActive: false);
                if (_destroyToken.IsCancellationRequested) return;
            
                float loadDuration = Time.realtimeSinceStartup - startLoadTime;

                if (loadDuration < _minDurationLoading) {
                    await UniTask.Delay(TimeSpan.FromSeconds(_minDurationLoading - loadDuration), cancellationToken: _destroyToken)
                        .SuppressCancellationThrow();
                    if (_destroyToken.IsCancellationRequested) return;
                }
            
                await Fader.Main.FadeInAsync(_fadeInToScene, _fadeInCurve.GetOrDefault());
                if (_destroyToken.IsCancellationRequested) return;
            }

            LoadingService.Instance.ShowLoadingScreen(false);
            SceneLoader.SetActiveScene(_levelsScene.scene);

            await Fader.Main.FadeOutAsync(fadeOutFinal, _fadeOutCurve.GetOrDefault());
        }

        public async UniTask ExitToMainMenu() {
            await Fader.Main.FadeInAsync(_fadeInToLoading, _fadeInCurve.GetOrDefault());
            if (_destroyToken.IsCancellationRequested) return;

            await SceneLoader.UnloadSceneAsync(_levelsScene.scene);
            if (_destroyToken.IsCancellationRequested) return;
            
            await SceneLoader.UnloadSceneAsync(_gameplayScene.scene);
            if (_destroyToken.IsCancellationRequested) return;
            
            float fadeOutFinal = _fadeOutToLoading;
            
            if (!SceneLoader.IsSceneLoaded(_menuScene.scene)) {
                fadeOutFinal = _fadeOutToScene;
                
                LoadingService.Instance.ShowLoadingScreen(true);
                
                await Fader.Main.FadeOutAsync(_fadeOutToLoading, _fadeOutCurve.GetOrDefault());
                if (_destroyToken.IsCancellationRequested) return;

                float startLoadTime = Time.realtimeSinceStartup;
            
                await SceneLoader.LoadSceneAsync(_menuScene.scene, makeActive: false);
                if (_destroyToken.IsCancellationRequested) return;
            
                float loadDuration = Time.realtimeSinceStartup - startLoadTime;

                if (loadDuration < _minDurationLoading) {
                    await UniTask.Delay(TimeSpan.FromSeconds(_minDurationLoading - loadDuration), cancellationToken: _destroyToken)
                        .SuppressCancellationThrow();
                    if (_destroyToken.IsCancellationRequested) return;
                }
            
                await Fader.Main.FadeInAsync(_fadeInToScene, _fadeInCurve.GetOrDefault());
                if (_destroyToken.IsCancellationRequested) return;
            }

            LoadingService.Instance.ShowLoadingScreen(false);
            SceneLoader.SetActiveScene(_menuScene.scene);
            
            await Fader.Main.FadeOutAsync(fadeOutFinal, _fadeOutCurve.GetOrDefault());
        }

        [Button]
        private void TestExit() {
            ExitToMainMenu().Forget();
        }
    }
    
}