using System.Collections.Generic;
using MisterGames.Collisions.Utils;
using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Common.Pooling;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Scripts.Runtime.Spider {
    
    public sealed class SpiderWeb : MonoBehaviour {

        [SerializeField] private LineRenderer _webLinePrefab;
        [SerializeField] [MinMaxSlider(0, 100)] private Vector2Int _spawnPointsCount;
        [SerializeField] [MinMaxSlider(0, 100)] private Vector2Int _raysCount;
        [SerializeField] [MinMaxSlider(0f, 2f)] private Vector2 _rayWidth;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] [Min(0f)] private float _maxSearchDistance;
        [SerializeField] [Min(0f)] private float _maxNextPointDistance;
        [SerializeField] [Min(0f)] private float _maxWebLineDistance;
        [SerializeField] [Min(0f)] private float _spawnPointElevation = 0.02f;
        [SerializeField] [Min(1)] private int _maxHits = 6;
        [SerializeField] [Min(0)] private int _maxRetryAttempts = 20;

        private static readonly Vector3[] Directions = {
            Vector3.up,
            Vector3.down,
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back,
        };
        
        private RaycastHit[] _hits;
        
        private void Awake() {
            _hits = new RaycastHit[_maxHits];
        }

        private bool PlaceWeb(Vector3 position, List<LineRenderer> dest) {
            dest.Clear();

            var pos = position;
            var rot = transform.rotation;
            
            int pointsCount = _spawnPointsCount.GetRandomInRange();
            bool spawnedAtLeastOne = false;
            
            for (int i = 0; i < pointsCount; i++) {
                if (!TryGetClosestPointOnSurface(pos, rot, out var point) &&
                    !TryGetClosestPointOnSurface(pos, rot * Quaternion.Euler(45f, 45f, 0f), out point)) 
                {
                    continue;
                }
            
                spawnedAtLeastOne |= PlaceWebFromPoint(point, dest);
                
                var newPos = position + Random.insideUnitSphere * _maxNextPointDistance;
                pos = Raycast(pos, (newPos - pos).normalized, _maxNextPointDistance, out var hit)
                    ? hit.point
                    : newPos;
            }
            
            return spawnedAtLeastOne;
        }

        private bool PlaceWebFromPoint(Vector3 point, List<LineRenderer> dest) {
            int raysCount = _raysCount.GetRandomInRange();
            bool spawnedAtLeastOne = false;
            
            for (int i = 0; i < raysCount; i++) {
                int attempts = 0;
                bool canPlaceRay = false;
                var endPoint = Vector3.zero;
                
                while (attempts++ < _maxRetryAttempts && !canPlaceRay) {
                    canPlaceRay = Raycast(point, Random.onUnitSphere, _maxWebLineDistance, out var hit);
                    endPoint = hit.point;
                }
                
                if (!canPlaceRay) continue;

                var line = CreateLine();
                line.SetPosition(0, point);
                line.SetPosition(1, endPoint);

                line.startWidth = 0f;
                line.endWidth = _rayWidth.GetRandomInRange() * (endPoint - point).magnitude;

                spawnedAtLeastOne = true;
                dest.Add(line);
            }

            return spawnedAtLeastOne;
        }
        
        private LineRenderer CreateLine() {
#if UNITY_EDITOR
            return PrefabUtility.InstantiatePrefab(_webLinePrefab, transform) as LineRenderer;
#endif

            return PrefabPool.Main.Get(_webLinePrefab, transform);
        }

        private bool TryGetClosestPointOnSurface(Vector3 pos, Quaternion rot, out Vector3 point) {
            float minSqrDistance = float.MaxValue;
            point = Vector3.zero;
            
            for (int i = 0; i < Directions.Length; i++) {
                var dir = Directions[i];
                if (!Raycast(pos, rot * dir, _maxSearchDistance, out var hit)) continue;
                
                float sqrDistance = (hit.point - pos).sqrMagnitude;
                if (sqrDistance > minSqrDistance) continue;
                
                minSqrDistance = sqrDistance;
                point = hit.point + hit.normal * _spawnPointElevation;
            }
            
            return minSqrDistance < float.MaxValue;
        }

        private bool Raycast(Vector3 origin, Vector3 dir, float distance, out RaycastHit hit) {
            int hitCount = Physics.RaycastNonAlloc(origin, dir, _hits, distance, _layerMask, QueryTriggerInteraction.Ignore);
            hit = default;

            return _hits.RemoveInvalidHits(ref hitCount)
                .TryGetMinimumDistanceHit(hitCount, out hit);
        }

#if UNITY_EDITOR
        [SerializeField] private List<LineRenderer> _editorLines = new();
        
        [Button]
        private void PlaceWeb() {
            RemoveWeb();
            
            _editorLines ??= new List<LineRenderer>();
            _hits ??= new RaycastHit[_maxHits];
            
            PlaceWeb(transform.position, _editorLines);
            
            EditorUtility.SetDirty(gameObject);
        }
        
        [Button]
        private void RemoveWeb() {
            for (int i = 0; i < _editorLines?.Count; i++) {
                if (_editorLines[i] is {} lineRenderer) DestroyImmediate(lineRenderer.gameObject);
            }
            
            _editorLines?.Clear();
            
            EditorUtility.SetDirty(gameObject);
        }
#endif
    }
    
}