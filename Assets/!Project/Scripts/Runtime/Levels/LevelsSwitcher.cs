using MisterGames.Character.Core;
using MisterGames.Common.Strings;
using MisterGames.Scenes.Core;
using UnityEngine;

namespace _Project.Scripts.Runtime.Levels {
    
    public sealed class LevelsSwitcher : MonoBehaviour {
        
        [SerializeField] private CharacterSpawnPoint _defaultSpawnPoint;
        [SerializeField] private bool _enableLevelOnAwake = true;
        [SerializeField] private LevelRoot[] _levels;
        
        private void Awake() {
#if UNITY_EDITOR
            if (!_launchFirstActiveLevelInEditor &&
                SceneLoader.LaunchMode == ApplicationLaunchMode.FromCustomEditorScene ||
                !TryGetFirstActiveLevel(out int level)) 
            {
                return;
            }

            LogWarning($"launching first active level in editor: level {level}.");
            LevelService.Instance.CurrentLevel = level;
#endif
            
            if (_enableLevelOnAwake) EnableLevel(LevelService.Instance.CurrentLevel);
        }

        private void OnEnable() {
            if (LevelService.Instance is {} levelService) levelService.OnLevelRequested += OnLevelRequested;
        }

        private void OnDisable() {
            if (LevelService.Instance is {} levelService) levelService.OnLevelRequested -= OnLevelRequested;
        }

        private void OnLevelRequested(int level) {
            EnableLevel(level);
        }

        private void EnableLevel(int level) {
            if (level < 0 || level >= _levels.Length) {
                LogError($"requested level #{level} is not found, levels total {_levels.Length}.");
                return;
            }

            for (int i = 0; i < _levels.Length; i++) {
                if (i != level) _levels[i].EnableLevel(false);
            }
            
            _levels[level].EnableLevel(true);
            _levels[level].SpawnOnLevel(_defaultSpawnPoint.transform);
            
            Log($"enabled level #{level}.");
        }

        private static void Log(string message) {
            Debug.Log($"{nameof(LevelsSwitcher).FormatColorOnlyForEditor(Color.white)}: f {Time.frameCount}, {message}");
        }

        private static void LogWarning(string message) {
            Debug.LogWarning($"{nameof(LevelsSwitcher).FormatColorOnlyForEditor(Color.white)}: f {Time.frameCount}, {message}");
        }
        
        private static void LogError(string message) {
            Debug.LogError($"{nameof(LevelsSwitcher).FormatColorOnlyForEditor(Color.white)}: f {Time.frameCount}, {message}");
        }
        
#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _launchFirstActiveLevelInEditor;
        
        private bool _isInvalidLevelsActivatedWarningShown;

        private bool TryGetFirstActiveLevel(out int level) {
            for (int i = 0; i < _levels.Length; i++) {
                if (!_levels[i].IsLevelActive()) continue;
                
                level = i;
                return true;
            }

            level = -1;
            return false;
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
                    LogWarning($"current level {currentLevel}, active levels {activeLevels}, " +
                               $"waiting for switching to one level to setup current level correctly.");
                }
                
                _isInvalidLevelsActivatedWarningShown = true;
                return;
            }
            
            _isInvalidLevelsActivatedWarningShown = false;
            
            // Valid
            if (currentLevel == firstActiveLevel) return;

            LogWarning($"current level {currentLevel} was switched to level {firstActiveLevel} manually.");
            
            LevelService.Instance.CurrentLevel = firstActiveLevel;
        }
#endif
    }
    
}