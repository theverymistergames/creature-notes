using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Common.Pooling;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace _Project.Scripts.Runtime.Enemies.Painting {
    
    public sealed class PaintingDecalPlacer : MonoBehaviour {

        [SerializeField] private MeshRenderer _paintingPlane;
        [SerializeField] private DecalProjector _decalProjectorPrefab;
        [SerializeField] private Vector3 _planeNormal = Vector3.right;
        [SerializeField] private float _scaleMul = 10f;
        [SerializeField] private float _scaleZ = 1f;

        [ReadOnly]
        [SerializeField] private DecalProjector _decal;
        
        private Material _decalMaterialInstance;

        private void Awake() {
            PlaceDecal();
        }

        private void OnDestroy() {
            RemoveDecal();
        }

        private DecalProjector CreateDecal() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                var decal = PrefabUtility.InstantiatePrefab(_decalProjectorPrefab, gameObject.scene) as DecalProjector;
                Undo.RegisterCreatedObjectUndo(decal, "PlaceDecal");
                return decal;
            }
#endif

            return PrefabPool.Main.Get(_decalProjectorPrefab);
        }

        [Button]
        private void PlaceDecal() {
            if (_decalProjectorPrefab == null || _paintingPlane == null) 
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RecordObject(this, "PlaceDecal");
#endif
            
            if (_decal == null) _decal = CreateDecal();

            var t = _decal!.transform;
            
#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RecordObject(t, "PlaceDecal"); 
#endif
            
            t.SetParent(null);
            t.SetLocalPositionAndRotation(default, Quaternion.identity);
            t.localScale = _decalProjectorPrefab.transform.localScale;
            
            _paintingPlane.transform.GetLocalPositionAndRotation(out var pos, out var rot);
            var forward = rot * _planeNormal;
            var scale = _paintingPlane.transform.localScale.Multiply(new Vector3(_scaleMul, _scaleMul, _scaleMul * _scaleZ));
            
            t.SetParent(_paintingPlane.transform.parent, worldPositionStays: false);
            t.SetLocalPositionAndRotation(pos, rot);
            t.localScale = scale;
            t.forward = forward;
            
            if (_decalMaterialInstance == null) {
                _decalMaterialInstance = new Material(_decalProjectorPrefab.material);
                
#if UNITY_EDITOR
                if (!Application.isPlaying) {
                    _decalMaterialInstance.name = $"{_decalProjectorPrefab.material.name} (instance)";
                    Undo.RegisterCreatedObjectUndo(_decalMaterialInstance, "PlaceDecal");
                }
#endif
            }
            
#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.RecordObject(_decal, "PlaceDecal"); 
#endif

            _decalMaterialInstance.mainTexture = _paintingPlane.sharedMaterial.mainTexture;
            _decal.material = _decalMaterialInstance;

#if UNITY_EDITOR
            if (!Application.isPlaying) {
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(t);
                EditorUtility.SetDirty(_decal);
                EditorUtility.SetDirty(_decalMaterialInstance);
            } 
#endif
        }
        
        [Button]
        private void RemoveDecal() {
            if (_decal == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying) {
                Undo.RecordObject(this, "RemoveDecal");
                
                if (_decalMaterialInstance != null) Undo.DestroyObjectImmediate(_decalMaterialInstance);
                Undo.DestroyObjectImmediate(_decal.gameObject);

                _decalMaterialInstance = null;
                _decal = null;
                
                EditorUtility.SetDirty(this);
                return;
            }      
#endif
            
            if (_decalMaterialInstance != null) Destroy(_decalMaterialInstance);
            PrefabPool.Main?.Release(_decal);
            
            _decalMaterialInstance = null;
            _decal = null;
        }
    }
    
}