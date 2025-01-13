using System;
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
    
    public sealed class SpiderWebPlacer : MonoBehaviour, IUpdate {

        [SerializeField] private SpiderWebLine _webLinePrefab;
        
        [Header("Spawn")]
        [SerializeField] [MinMaxSlider(0, 100)] private Vector2Int _spawnPointsCount;
        [SerializeField] [MinMaxSlider(0, 100)] private Vector2Int _raysCount;
        [SerializeField] [MinMaxSlider(0f, 2f)] private Vector2 _rayWidth;
        [SerializeField] private AnimationCurve _spawnScaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private SpawnMode _spawnMode;
        [SerializeField] private BoxCollider[] _boxBounds;
        
        [Header("Detection")]
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] [Min(0f)] private float _maxSearchDistance;
        [SerializeField] [Min(0f)] private float _maxNextPointDistance;
        [SerializeField] [Min(0f)] private float _maxWebLineDistance;
        [SerializeField] [Min(1)] private int _maxHits = 6;
        [SerializeField] [Min(0)] private int _maxRetryAttempts = 20;

        [Header("Burn")]
        [SerializeField] private BurnMode _burnMode = BurnMode.LinesFromOnePoint;
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _burnTimeRange;

        private enum SpawnMode {
            AutoUpdate,
            ManualUpdate,
        }
        
        private enum BurnMode {
            AllLines,
            LinesFromOnePoint,
            SingleLine,
        }
        
        private readonly struct LineAnimationData {
            
            public readonly LineRenderer line;
            public readonly Transform transform;
            public readonly Vector2 time;
            public readonly Vector2 progress;
            public readonly Vector2 width;
            public readonly Vector2 distance;
            
            public LineAnimationData(LineRenderer line, Vector2 time, Vector2 progress, Vector2 width, Vector2 distance) {
                this.line = line;
                transform = line.transform;
                this.time = time;
                this.progress = progress;
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
        private readonly List<SpiderWebLine> _spiderWebLines = new();
        private RaycastHit[] _hits;
        
        private void Awake() {
            _hits = new RaycastHit[_maxHits];
        }

        private void OnDisable() {
            _animationList.Clear();
            _spiderWebLines.Clear();
            
            PlayerLoopStage.Update.Unsubscribe(this);
        }

        public void BurnWeb() {
            for (int i = 0; i < _spiderWebLines.Count; i++) {
                var line = _spiderWebLines[i];
                if (line != null) line.Burn();
            }
            
            _spiderWebLines.Clear();
            _animationList.Clear();
        }

        public Vector3 GetRandomSpawnPosition() {
            if (_boxBounds.Length == 0) return transform.position;
            
            int i = Random.Range(0, _boxBounds.Length);
            var box = _boxBounds[i];
            var local = RandomExtensions.GetRandomPointInBox(box.size * 0.5f);
            
            return box.bounds.center + box.transform.rotation * local;
        }
        
        public void PlaceWeb(Vector3 position, Vector2 spawnDurationRange) {
            var pos = position;
            var rot = transform.rotation;
            
            int pointsCount = _spawnPointsCount.GetRandomInRange();
            SpiderWebLine prevLine = null;
            
            for (int i = 0; i < pointsCount; i++) {
                if (!TryGetClosestPointOnSurface(pos, rot, out var hit) &&
                    !TryGetClosestPointOnSurface(pos, rot * Quaternion.Euler(45f, 45f, 0f), out hit)) 
                {
                    continue;
                }
            
                prevLine = PlaceWebFromPoint(hit.point, hit.normal, spawnDurationRange, prevLine);
                
                var newPos = position + Random.insideUnitSphere * _maxNextPointDistance;
                pos = Raycast(pos, (newPos - pos).normalized, _maxNextPointDistance, out hit)
                    ? hit.point
                    : newPos;
            }
            
            if (_spawnMode == SpawnMode.AutoUpdate) PlayerLoopStage.Update.Subscribe(this);
        }
        
        public void SetSpawnProgressManual(float progress) {
            if (_spawnMode != SpawnMode.ManualUpdate) return;
            
            for (int i = 0; i < _animationList.Count; i++) {
                var data = _animationList[i];
                SetSpawnProgress(ref data, progress);
            }
        }
        
        private SpiderWebLine PlaceWebFromPoint(Vector3 point, Vector3 normal, Vector2 spawnDurationRange, SpiderWebLine prevLine) {
            int raysCount = _raysCount.GetRandomInRange();
            SpiderWebLine line = null;
            
            prevLine = _burnMode switch {
                BurnMode.AllLines => prevLine,
                BurnMode.LinesFromOnePoint => null,
                BurnMode.SingleLine => null,
                _ => throw new ArgumentOutOfRangeException()
            };
            
            for (int i = 0; i < raysCount; i++) {
                int attempts = 0;
                bool canPlaceRay = false;
                var endPoint = Vector3.zero;
                
                while (attempts++ < _maxRetryAttempts && !canPlaceRay) {
                    canPlaceRay = Raycast(point, GetRandomDir(normal), _maxWebLineDistance, out var hit);
                    endPoint = hit.point;
                }
                
                if (!canPlaceRay) continue;

                line = CreateLine();
                
                line.Restore();
                line.SetBurnTimeRange(_burnTimeRange);

                if (_burnMode != BurnMode.SingleLine && prevLine is not null) {
                    prevLine.SetNextNode(line);
                    line.SetPreviousNode(prevLine);
                }
                else {
                    line.SetPreviousNode(null);
                    line.SetNextNode(null);
                }
                
                prevLine = line;
                SpawnLine(line.Line, point, endPoint, spawnDurationRange);
                
                _spiderWebLines.Add(line);
            }

            return line;
        }

        private void SpawnLine(LineRenderer line, Vector3 start, Vector3 end, Vector2 spawnDurationRange) {
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

            float startTime = TimeSources.time;
            float finishTime = startTime + spawnDurationRange.y;
            
            float startProgress = spawnDurationRange.y > 0f 
                ? Random.Range(0f, 1f - spawnDurationRange.x / spawnDurationRange.y)
                : 0f;
            float endProgress = spawnDurationRange.y > 0f 
                ? Random.Range(startProgress + spawnDurationRange.x / spawnDurationRange.y, 1f)
                : 0f;

            var data = new LineAnimationData(
                line,
                new Vector2(startTime, finishTime), 
                new Vector2(startProgress, endProgress), 
                new Vector2(0f, width), 
                new Vector2(0f, distance)
            );
            
            _animationList.Add(data);

#if UNITY_EDITOR
            if (!Application.isPlaying) EditorUtility.SetDirty(t);
            if (!Application.isPlaying) EditorUtility.SetDirty(line);
#endif
        }

        void IUpdate.OnUpdate(float dt) {
            float time = TimeSources.time;
            int finishedCount = 0;

            for (int i = 0; i < _animationList.Count; i++) {
                var data = _animationList[i];

#if UNITY_EDITOR
                if (data.line == null) {
                    finishedCount++;
                    continue;
                }
#endif
                
                float progress = data.time.y.IsNearlyEqual(data.time.x) 
                    ? 1f 
                    : (time - data.time.x) / (data.time.y - data.time.x);
                
                SetSpawnProgress(ref data, progress);
                
                if (progress >= 1f) finishedCount++;
            }
            
            if (finishedCount < _animationList.Count) return;
            
            _animationList.Clear();
            PlayerLoopStage.Update.Unsubscribe(this);
        }

        private void SetSpawnProgress(ref LineAnimationData data, float progress) {
            float t = data.progress.x.IsNearlyEqual(data.progress.y)
                ? 1f
                : Mathf.Clamp01((progress - data.progress.x) / (data.progress.y - data.progress.x));
            
            float p = _spawnScaleCurve.Evaluate(t);
                
            data.line.endWidth = Mathf.Lerp(data.width.x, data.width.y, p);
            data.transform.localScale = Vector3.Lerp(
                new Vector3(data.width.x, data.width.x, data.distance.x), 
                new Vector3(data.width.y, data.width.y, data.distance.y), 
                p
            );
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

        private SpiderWebLine CreateLine() {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                var line = PrefabUtility.InstantiatePrefab(_webLinePrefab, transform) as SpiderWebLine;
                Undo.RegisterCreatedObjectUndo(line, UndoKey);
                return line;   
            }
#endif

            return PrefabPool.Main.Get(_webLinePrefab, transform);
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
        private const string UndoKey = "SpiderWeb_PlaceWeb";

        [Header("Debug")]
        [SerializeField] [MinMaxSlider(0f, 10f)] private Vector2 _testSpawnDurationRange = new(1f, 2f);
        [SerializeField] [Range(0f, 1f)] private float _testManualProgress;
        
        private void OnValidate() {
            SetSpawnProgressManual(_testManualProgress);
        }

        [Button]
        private void PlaceWeb() {
            Undo.RecordObject(gameObject, UndoKey);
            
            RemoveWeb();
            
            _hits ??= new RaycastHit[_maxHits];
            
            PlaceWeb(transform.position, _testSpawnDurationRange);
            
            EditorUtility.SetDirty(gameObject);
        }
        
        [Button]
        private void RemoveWeb() {
            Undo.RecordObject(gameObject, UndoKey);

            _spiderWebLines.Clear();
            _animationList.Clear();
            
            var lines = GetComponentsInChildren<SpiderWebLine>();
            for (int i = 0; i < lines.Length; i++) {
                if (lines[i] is {} line) Undo.DestroyObjectImmediate(line.gameObject);
            }
            
            EditorUtility.SetDirty(gameObject);
        }

        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void BurnAllWeb() {
            var lines = GetComponentsInChildren<SpiderWebLine>();
            _spiderWebLines.AddRange(lines);
            
            BurnWeb();
        }
#endif
    }
    
}