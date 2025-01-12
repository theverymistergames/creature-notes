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
                if (!TryGetClosestPointOnSurface(pos, rot, out var hit) &&
                    !TryGetClosestPointOnSurface(pos, rot * Quaternion.Euler(45f, 45f, 0f), out hit)) 
                {
                    continue;
                }
            
                spawnedAtLeastOne |= PlaceWebFromPoint(hit.point, hit.normal, dest);
                
                var newPos = position + Random.insideUnitSphere * _maxNextPointDistance;
                pos = Raycast(pos, (newPos - pos).normalized, _maxNextPointDistance, out hit)
                    ? hit.point
                    : newPos;
            }
            
            return spawnedAtLeastOne;
        }

        private bool PlaceWebFromPoint(Vector3 point, Vector3 normal, List<LineRenderer> dest) {
            int raysCount = _raysCount.GetRandomInRange();
            bool spawnedAtLeastOne = false;
            
            for (int i = 0; i < raysCount; i++) {
                int attempts = 0;
                bool canPlaceRay = false;
                var endPoint = Vector3.zero;
                
                while (attempts++ < _maxRetryAttempts && !canPlaceRay) {
                    canPlaceRay = Raycast(point, GetRandomDir(normal), _maxWebLineDistance, out var hit);
                    endPoint = hit.point;
                }
                
                if (!canPlaceRay) continue;

                var line = CreateLine();
                PlaceLine(line, point, endPoint);

                spawnedAtLeastOne = true;
                dest.Add(line);
            }

            return spawnedAtLeastOne;
        }

        private void PlaceLine(LineRenderer line, Vector3 start, Vector3 end) {
            float distance = Vector3.Distance(start, end);
            float width = _rayWidth.GetRandomInRange() * distance;
            
            var t = line.transform;
            t.SetPositionAndRotation(start, Quaternion.LookRotation(end - start));
            t.localScale = new Vector3(width, width, distance);
            
            line.useWorldSpace = false;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.forward);

            line.startWidth = 0f;
            line.endWidth = width;
        }
        
        private LineRenderer CreateLine() {
#if UNITY_EDITOR
            return PrefabUtility.InstantiatePrefab(_webLinePrefab, transform) as LineRenderer;
#endif

            return PrefabPool.Main.Get(_webLinePrefab, transform);
        }

        private bool TryGetClosestPointOnSurface(Vector3 pos, Quaternion rot, out RaycastHit hit) {
            float minSqrDistance = float.MaxValue;
            hit = default;
            
            for (int i = 0; i < Directions.Length; i++) {
                var dir = Directions[i];
                if (!Raycast(pos, rot * dir, _maxSearchDistance, out var h)) continue;
                
                float sqrDistance = (h.point - pos).sqrMagnitude;
                if (sqrDistance > minSqrDistance) continue;
                
                minSqrDistance = sqrDistance;
                hit = h;
            }
            
            return minSqrDistance < float.MaxValue;
        }

        private bool Raycast(Vector3 origin, Vector3 dir, float distance, out RaycastHit hit) {
            int hitCount = Physics.RaycastNonAlloc(origin, dir, _hits, distance, _layerMask, QueryTriggerInteraction.Ignore);
            hit = default;

            return _hits.RemoveInvalidHits(ref hitCount)
                .TryGetMinimumDistanceHit(hitCount, out hit);
        }

        private static Vector3 GetRandomDir(Vector3 normal) {
            var dir = Random.onUnitSphere;
            return Vector3.Dot(dir, normal) > 0f ? dir : -dir;
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