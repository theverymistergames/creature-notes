using MisterGames.Character.Core;
using MisterGames.Scenes.Core;
using UnityEngine;

namespace _Project.Scripts.Runtime.Levels {
    
    public sealed class LevelsSwitcher : MonoBehaviour {
        
        [SerializeField] private CharacterSpawnPoint _defaultSpawnPoint;
        [SerializeField] private LevelRoot[] _levels;

        private void Awake() {
#if UNITY_EDITOR
            TryFetchFirstActiveLevel();
#endif
            
            SetLevel(LevelService.Instance.CurrentLevel);
        }

        private void OnEnable() {
            if (LevelService.Instance is {} levelService) levelService.OnLevelRequested += OnLevelRequested;
        }

        private void OnDisable() {
            if (LevelService.Instance is {} levelService) levelService.OnLevelRequested -= OnLevelRequested;
        }

        private void OnLevelRequested(int level) {
            SetLevel(level);
        }

        private void SetLevel(int level) {
            if (level < 0 || level >= _levels.Length) {
                Debug.LogError($"{nameof(LevelsSwitcher)}: requested level #{level} is not found, levels total {_levels.Length}");
                return;
            }

            for (int i = 0; i < _levels.Length; i++) {
                _levels[i].EnableLevel(false);
            }
            
            _levels[level].EnableLevel(true);
            _levels[level].SpawnOnLevel(_defaultSpawnPoint.transform);
            
            Debug.Log($"{nameof(LevelsSwitcher)}: enabled level #{level}");
        }

#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _useFirstActiveLevelSetFromEditor = true;
        
        private bool _isInvalidLevelsActivatedWarningShown;

        private void TryFetchFirstActiveLevel() {
            if (!_useFirstActiveLevelSetFromEditor || SceneLoader.LaunchMode != ApplicationLaunchMode.FromCustomEditorScene) {
                return;
            } 
            
            for (int i = 0; i < _levels.Length; i++) {
                if (!_levels[i].IsLevelActive()) continue;

                if (LevelService.Instance is {} levelService) levelService.CurrentLevel = i;
                return;
            }
        }
        
        private void Update() {
            int currentLevel = LevelService.Instance.CurrentLevel;
            int activeLevels = 0;
            int firstActiveLevel = -1;
            
            for (int i = 0; i < _levels.Length; i++) {
                if (!_levels[i].IsLevelActive()) continue;
                
                activeLevels++;
                if (firstActiveLevel < 0) firstActiveLevel = i;
            }
            
            // Wait before user switches to one active level
            if (activeLevels is <= 0 or > 1) {
                if (!_isInvalidLevelsActivatedWarningShown) {
                    Debug.LogWarning($"{nameof(LevelsSwitcher)}: f {Time.frameCount}, current level {currentLevel}, active levels {activeLevels}, " +
                                     $"waiting for switching to one level to setup current level correctly.");
                }
                
                _isInvalidLevelsActivatedWarningShown = true;
                return;
            }
            
            _isInvalidLevelsActivatedWarningShown = false;
            
            // Valid
            if (currentLevel == firstActiveLevel) return;

            Debug.LogWarning($"{nameof(LevelsSwitcher)}: f {Time.frameCount}, current level {currentLevel} was switched to level {firstActiveLevel} manually.");
            
            LevelService.Instance.CurrentLevel = firstActiveLevel;
        }
#endif
    }
    
}