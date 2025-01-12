using System.Collections.Generic;
using MisterGames.Collisions.Utils;
using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Common.Pooling;
using MisterGames.Common.Tick;
using UnityEngine;
using Random = UnityEngine.Random;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Scripts.Runtime.Spider {
    
    public sealed class SpiderWeb : MonoBehaviour, IUpdate {

        [SerializeField] private LineRenderer _webLinePrefab;
        
        [Header("Spawn")]
        [SerializeField] [MinMaxSlider(0, 100)] private Vector2Int _spawnPointsCount;
        [SerializeField] [MinMaxSlider(0, 100)] private Vector2Int _raysCount;
        [SerializeField] [MinMaxSlider(0f, 2f)] private Vector2 _rayWidth;
        [SerializeField] [Min(0f)] private float _spawnDelayMax = 1;
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _spawnDurationRange;
        [SerializeField] private AnimationCurve _spawnScaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [Header("Detection")]
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] [Min(0f)] private float _maxSearchDistance;
        [SerializeField] [Min(0f)] private float _maxNextPointDistance;
        [SerializeField] [Min(0f)] private float _maxWebLineDistance;
        [SerializeField] [Min(1)] private int _maxHits = 6;
        [SerializeField] [Min(0)] private int _maxRetryAttempts = 20;

        private readonly struct LineAnimationData {
            
            public readonly LineRenderer line;
            public readonly Transform transform;
            public readonly float startTime;
            public readonly float endTime;
            public readonly float width;
            public readonly float distance;
            
            public LineAnimationData(LineRenderer line, float startTime, float endTime, float width, float distance) {
                this.line = line;
                transform = line.transform;
                this.startTime = startTime;
                this.endTime = endTime;
                this.width = width;
                this.distance = distance;
            }
        }
        
        private static readonly Vector3[] Directions = {
            Vector3.up,
            Vector3.down,
            Vector3.right,
            Vector3.left,
            Vector3.forward,
            Vector3.back,
        };

        private readonly List<LineAnimationData> _animationList = new();
        private RaycastHit[] _hits;
        
        private void Awake() {
            _hits = new RaycastHit[_maxHits];
        }

        private void OnDisable() {
            _animationList.Clear();
            PlayerLoopStage.Update.Unsubscribe(this);
        }

        private bool PlaceWeb(Vector3 position, List<LineRenderer> dest) {
            dest.Clear();

            var pos = position;
            var rot = transform.rotation;
            
            int pointsCount = _spawnPointsCount.GetRandomInRange();
            bool spawnedAtLeastOne = false;
            
            _animationList.Clear();
            
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
            
            PlayerLoopStage.Update.Subscribe(this);
            
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
            t.localScale = Vector3.zero;
            
            line.useWorldSpace = false;
            line.SetPosition(0, Vector3.zero);
            line.SetPosition(1, Vector3.forward);

            line.startWidth = 0f;
            line.endWidth = 0f;

            float startTime = Time.time + Random.Range(0f, _spawnDelayMax);
            float endTime = startTime + _spawnDurationRange.GetRandomInRange();

            _animationList.Add(new LineAnimationData(line, startTime, endTime, width, distance));
        }

        void IUpdate.OnUpdate(float dt) {
            float time = Time.time;
            int finishedCount = 0;
            
            for (int i = 0; i < _animationList.Count; i++) {
                var data = _animationList[i];
                
                float t = data.endTime.IsNearlyEqual(data.startTime) 
                    ? 1f 
                    : Mathf.Clamp01(time - data.startTime) / (data.endTime - data.startTime);
                float p = _spawnScaleCurve.Evaluate(t);
                
                data.line.endWidth = Mathf.Lerp(0f, data.width, p);
                data.transform.localScale = Vector3.Lerp(Vector3.zero, new Vector3(data.width, data.width, data.distance), p);
                
                if (t >= 1f) finishedCount++;
            }
            
            if (finishedCount < _animationList.Count) return;
            
            _animationList.Clear();
            PlayerLoopStage.Update.Unsubscribe(this);
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