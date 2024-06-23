using MisterGames.Common.GameObjects;
using UnityEngine;

namespace _Project.Scripts.Runtime.Telescope {
    
    public static class StarGroupUtils {
    
        public static TransformData GetTransformDataPlacedInCenter(
            TransformData initialData,
            Quaternion rotationOffset,
            float scale,
            float groupScale,
            Vector3 groupCenter) 
        {
            return new TransformData(
                rotationOffset * (initialData.position - groupCenter) * groupScale,
                initialData.rotation * Quaternion.Euler(0f, 0f, rotationOffset.eulerAngles.z),
                initialData.scale * scale
            );
        }

        public static Vector3 GetStarGroupCenter(TransformData[] objects) {
            if (objects is not { Length: > 0 }) return Vector3.zero;
            
            var center = Vector3.zero;
                
            for (int j = 0; j < objects.Length; j++) {
                center += objects[j].position;
            }

            return center / objects.Length;
        }
    }
    
}