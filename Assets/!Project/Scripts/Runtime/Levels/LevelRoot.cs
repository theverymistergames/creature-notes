using MisterGames.Character.Core;
using MisterGames.Character.Motion;
using UnityEngine;

namespace _Project.Scripts.Runtime.Levels {
    
    public sealed class LevelRoot : MonoBehaviour {

        [SerializeField] private CharacterSpawnPoint _spawnPointMain;

        public bool IsLevelActive() {
            return gameObject.activeSelf;
        }
        
        public void EnableLevel(bool enable) {
            gameObject.SetActive(enable);
        }
        
        public void SpawnOnLevel(Transform defaultSpawnPoint) {
            TeleportTo(_spawnPointMain == null ? defaultSpawnPoint : _spawnPointMain.transform);
        }

        private static void TeleportTo(Transform spawnPoint) {
            spawnPoint.GetPositionAndRotation(out var position, out var rotation);
            
            CharacterSystem.Instance.GetCharacter()
                .GetComponent<CharacterMotionPipeline>()
                .Teleport(position, rotation, preserveVelocity: false);
        }
    }
    
}