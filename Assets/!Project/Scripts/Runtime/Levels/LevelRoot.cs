using MisterGames.Character.Core;
using MisterGames.Character.Motion;
using MisterGames.Scenes.Core;
using UnityEngine;

#if UNITY_EDITOR
using MisterGames.Scenes.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace _Project.Scripts.Runtime.Levels {
    
    [ExecuteInEditMode]
    public sealed class LevelRoot : MonoBehaviour {

        [SerializeField] private CharacterSpawnPoint _spawnPointMain;
        [SerializeField] private SceneReference _levelScene;
        
        public bool IsLevelActive() {
            return gameObject.activeSelf;
        }
        
        public void EnableLevel(bool enable) {
            gameObject.SetActive(enable);
        }
        
        public void SpawnOnLevel(Transform defaultSpawnPoint) {
            TeleportTo(_spawnPointMain == null ? defaultSpawnPoint : _spawnPointMain.transform);
        }
        
        private void OnEnable() {
#if UNITY_EDITOR
            if (!LevelLoadingOptions.AllowLevelLoadingInEditor || OpenSceneInEditor()) return;
#endif
            
            if (_levelScene.IsValid()) SceneLoader.LoadScene(_levelScene.scene, makeActive: true);
        }

        private void OnDisable() {
#if UNITY_EDITOR
            if (!LevelLoadingOptions.AllowLevelLoadingInEditor || CloseSceneInEditor()) return;
#endif
            
            if (_levelScene.IsValid()) SceneLoader.UnloadScene(_levelScene.scene);
        }
        
        private static void TeleportTo(Transform spawnPoint) {
            spawnPoint.GetPositionAndRotation(out var position, out var rotation);
            
            CharacterSystem.Instance.GetCharacter()
                .GetComponent<CharacterMotionPipeline>()
                .Teleport(position, rotation, preserveVelocity: false);
        }

#if UNITY_EDITOR
        private bool OpenSceneInEditor() {
            if (Application.isPlaying) return false;
            
            if (_levelScene.IsValid() && SceneLoaderSettings.GetSceneAsset(_levelScene.scene) is { } sceneAsset) {
                var scene = SceneManager.GetSceneByName(sceneAsset.name);
                
                if (!scene.isLoaded) {
                    scene = EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(sceneAsset), OpenSceneMode.Additive);
                }

                SceneManager.SetActiveScene(scene);
            }
            
            return true;
        }
        
        private bool CloseSceneInEditor() {
            if (Application.isPlaying) return false;
            
            if (_levelScene.IsValid() && SceneLoaderSettings.GetSceneAsset(_levelScene.scene) is { } sceneAsset) {
                var scene = SceneManager.GetSceneByName(sceneAsset.name);
                
                if (scene.isLoaded && SceneUtils.ShowSaveSceneDialog_EditorOnly(scene)) {
                    SceneManager.UnloadSceneAsync(scene);
                }
            }
            
            return true;
        }
#endif
    }
    
}