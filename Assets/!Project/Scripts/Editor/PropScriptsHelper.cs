using MisterGames.Collisions.Core;
using MisterGames.Common.Labels;
using MisterGames.Logic.Phys;
using MisterGames.Logic.Water;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Editor {
    
    internal static class PropScriptsHelper {

        [MenuItem("CONTEXT/Component/Setup PROP components")]
        private static void SetupPropScripts(MenuCommand menuCommand) {
            if (menuCommand.context is not Component c) return;
            
            var go = c.gameObject;
            var servicesLib = AssetDatabase.LoadAssetAtPath<LabelLibrary>("Assets/!Project/Services/ServicesLib.asset");
            
            if (!go.TryGetComponent(out RigidbodyCustomGravityGroupMember rigidbodyGroupMember)) {
                rigidbodyGroupMember = go.AddComponent<RigidbodyCustomGravityGroupMember>();
            }
            
            if (!go.TryGetComponent(out SurfaceMaterial surfaceMaterial)) {
                surfaceMaterial = go.AddComponent<SurfaceMaterial>();
            }
            
            if (!go.TryGetComponent(out CollisionBatchGroupMember collisionBatchGroupMember)) {
                collisionBatchGroupMember = go.AddComponent<CollisionBatchGroupMember>();
            }
            
            rigidbodyGroupMember.Group = new LabelValue(servicesLib, 2);
            rigidbodyGroupMember.KinematicOnAwake = true;
            EditorUtility.SetDirty(rigidbodyGroupMember);

            collisionBatchGroupMember.Group = new LabelValue(servicesLib, 5);
            EditorUtility.SetDirty(collisionBatchGroupMember);
            
            EditorUtility.SetDirty(go);
        }
        
        [MenuItem("CONTEXT/Component/Setup PROP components with buoyancy +1")]
        private static void SetupPropScriptsWithBuoyancy1(MenuCommand menuCommand) {
            if (menuCommand.context is not Component c) return;
            
            var go = c.gameObject;
            var servicesLib = AssetDatabase.LoadAssetAtPath<LabelLibrary>("Assets/!Project/Services/ServicesLib.asset");
            
            if (!go.TryGetComponent(out RigidbodyCustomGravityGroupMember rigidbodyGroupMember)) {
                rigidbodyGroupMember = go.AddComponent<RigidbodyCustomGravityGroupMember>();
            }
            
            if (!go.TryGetComponent(out SurfaceMaterial surfaceMaterial)) {
                surfaceMaterial = go.AddComponent<SurfaceMaterial>();
            }
            
            if (!go.TryGetComponent(out WaterClient waterClient)) {
                waterClient = go.AddComponent<WaterClient>();
            }
            
            if (!go.TryGetComponent(out CollisionBatchGroupMember collisionBatchGroupMember)) {
                collisionBatchGroupMember = go.AddComponent<CollisionBatchGroupMember>();
            }
            
            rigidbodyGroupMember.Group = new LabelValue(servicesLib, 2);
            rigidbodyGroupMember.KinematicOnAwake = true;
            EditorUtility.SetDirty(rigidbodyGroupMember);

            waterClient.Buoyancy = 1f;
            EditorUtility.SetDirty(collisionBatchGroupMember);
            
            collisionBatchGroupMember.Group = new LabelValue(servicesLib, 5);
            EditorUtility.SetDirty(collisionBatchGroupMember);
            
            EditorUtility.SetDirty(go);
        }
        
        [MenuItem("CONTEXT/Component/Setup PROP components with buoyancy -0.5")]
        private static void SetupPropScriptsWithBuoyancyMinusDot5(MenuCommand menuCommand) {
            if (menuCommand.context is not Component c) return;
            
            var go = c.gameObject;
            var servicesLib = AssetDatabase.LoadAssetAtPath<LabelLibrary>("Assets/!Project/Services/ServicesLib.asset");
            
            if (!go.TryGetComponent(out RigidbodyCustomGravityGroupMember rigidbodyGroupMember)) {
                rigidbodyGroupMember = go.AddComponent<RigidbodyCustomGravityGroupMember>();
            }
            
            if (!go.TryGetComponent(out SurfaceMaterial surfaceMaterial)) {
                surfaceMaterial = go.AddComponent<SurfaceMaterial>();
            }
            
            if (!go.TryGetComponent(out WaterClient waterClient)) {
                waterClient = go.AddComponent<WaterClient>();
            }
            
            if (!go.TryGetComponent(out CollisionBatchGroupMember collisionBatchGroupMember)) {
                collisionBatchGroupMember = go.AddComponent<CollisionBatchGroupMember>();
            }
            
            rigidbodyGroupMember.Group = new LabelValue(servicesLib, 2);
            rigidbodyGroupMember.KinematicOnAwake = true;
            EditorUtility.SetDirty(rigidbodyGroupMember);

            waterClient.Buoyancy = -1f;
            EditorUtility.SetDirty(collisionBatchGroupMember);
            
            collisionBatchGroupMember.Group = new LabelValue(servicesLib, 5);
            EditorUtility.SetDirty(collisionBatchGroupMember);
            
            EditorUtility.SetDirty(go);
        }
    }
    
}