using System;
using System.Collections.Generic;
using Deform;
using MisterGames.Common;
using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Common.Pooling;
using MisterGames.Tick.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Flesh {
    
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FleshBubbleSpawner : MonoBehaviour, IUpdate {

        [SerializeField] private FleshController _fleshController;
        [SerializeField] private Deformable _deformable;
        
        [Header("Spawn Settings")]
        [SerializeField] private SpherifyDeformer _spherePrefab;
        [SerializeField] private GameObject _sphereExplosionVfx;
        [SerializeField] [Min(0f)] private float _sphereExplosionDuration = 0.1f;
        [SerializeField] [Min(0f)] private float _minRadiusToSpawnExplosion = 0.3f;
        [SerializeField] private float _maxSphereLevelAboveSurface = -0.1f;
        [SerializeField] private SpawnProfile[] _spawnProfiles;

        [Serializable]
        private struct SpawnProfile {
            
            [Header("Spawn")]
            [MinMaxSlider(0f, 1f)] public Vector2 progressRange;
            [MinMaxSlider(0f, 10f)] public Vector2 spawnDelayRange;
            [MinMaxSlider(0f, 10f)] public Vector2 lifetimeRange;
            
            [Header("Burst")]
            [Range(0f, 1f)] public float burstChance;
            [Min(0)] public int maxBurst;
            [MinMaxSlider(0f, 10f)] public Vector2 burstDistanceRange;
            
            [Header("Positioning")]
            public Vector2 excludeCenter;
            [MinMaxSlider(0f, 2f)] public Vector2 startScaleRange;
            [MinMaxSlider(0f, 2f)] public Vector2 endScaleRange;
            [MinMaxSlider(-2f, 2f)] public Vector2 startSpeedRangeUp;
            [MinMaxSlider(-2f, 2f)] public Vector2 endSpeedRangeUp;
            [MinMaxSlider(0f, 2f)] public Vector2 startSpeedRangeSide;
            [MinMaxSlider(0f, 2f)] public Vector2 endSpeedRangeSide;

            [Header("Debug")]
            public bool showDebugInfo;
        }

        private readonly struct BubbleData {
            
            public readonly SpherifyDeformer deformer;
            public readonly Transform transform;
            public readonly float createTime;
            public readonly float lifetime;
            public readonly float startScale;
            public readonly float endScale;
            public readonly Vector3 startSpeed;
            public readonly Vector3 endSpeed;
            
            public BubbleData(
                SpherifyDeformer deformer,
                float createTime,
                float lifetime,
                float startScale,
                float endScale,
                Vector3 startSpeed,
                Vector3 endSpeed) 
            {
                this.deformer = deformer;
                transform = deformer.transform;
                this.createTime = createTime;
                this.lifetime = lifetime;
                this.startScale = startScale;
                this.endScale = endScale;
                this.startSpeed = startSpeed;
                this.endSpeed = endSpeed;
                this.deformer = deformer;
            }
        }
        
        private readonly List<BubbleData> _bubbles = new();
        private readonly HashSet<Transform> _explosions = new();
        private float[] _spawnTimes;
        private BoxCollider _boxCollider;
        private Transform _transform;
        
        private void Awake() {
            _transform = transform;
            
            _boxCollider = GetComponent<BoxCollider>();
            _boxCollider.isTrigger = true;
            
            _spawnTimes = new float[_spawnProfiles.Length];
        }

        private void OnDestroy() {
            for (int i = 0; i < _bubbles.Count; i++) {
                var bubbleData = _bubbles[i];
                _deformable.RemoveDeformer(bubbleData.deformer);
                PrefabPool.Main?.Release(bubbleData.transform);
            }
        }

        private void OnEnable() {
            PlayerLoopStage.Update.Subscribe(this);
        }

        private void OnDisable() {
            PlayerLoopStage.Update.Unsubscribe(this);
        }

        void IUpdate.OnUpdate(float dt) {
            ProcessSpawns();
            ProcessBubbles(dt);
        }

        private void ProcessSpawns() {
            float progress = _fleshController.Progress;
            if (progress <= 0f) return;
            
            float time = Time.time;
            
            for (int i = 0; i < _spawnProfiles?.Length; i++) {
                ref var spawnProfile = ref _spawnProfiles[i];
                ref float spawnTime = ref _spawnTimes[i];
                
                if (!progress.InRange(spawnProfile.progressRange) || spawnTime > time) continue;
                
                spawnTime = time + spawnProfile.spawnDelayRange.GetRandomInRange();
                Spawn(ref spawnProfile);
            }
        }

        private void ProcessBubbles(float dt) {
            float time = Time.time;
            
            for (int i = _bubbles.Count - 1; i >= 0; i--) {
                var bubbleData = _bubbles[i];
                float progress = bubbleData.lifetime > 0f ? Mathf.Clamp01((time - bubbleData.createTime) / bubbleData.lifetime) : 1f;
                
                float scale = Mathf.Lerp(bubbleData.startScale, bubbleData.endScale, progress);
                bubbleData.transform.localScale = scale * Vector3.one;
                
                if (progress >= 1f) {
                    if (_explosions.Add(bubbleData.transform) && 
                        bubbleData.endScale >= _minRadiusToSpawnExplosion &&
                        bubbleData.transform.localPosition.y > -bubbleData.endScale) 
                    {
                        var vfx = PrefabPool.Main.Get(_sphereExplosionVfx, bubbleData.transform.position, Quaternion.identity);
                        vfx.transform.localScale = bubbleData.transform.localScale;
                    }
                    
                    float t = _sphereExplosionDuration > 0f ? (time - bubbleData.createTime - bubbleData.lifetime) / _sphereExplosionDuration : 1f;
                    bubbleData.transform.localScale = Mathf.Lerp(bubbleData.endScale, 0f, t) * Vector3.one;

                    if (t >= 1f) {
                        _explosions.Remove(bubbleData.transform);
                        _bubbles.RemoveAt(i);
                        _deformable.RemoveDeformer(bubbleData.deformer);
                        PrefabPool.Main.Release(bubbleData.transform);
                    }
                    
                    continue;
                }
                
                var speed = Vector3.Lerp(bubbleData.startSpeed, bubbleData.endSpeed, progress);
                var position = bubbleData.transform.localPosition + speed * dt;
                
                position.y = Mathf.Clamp(position.y, -scale, scale * _maxSphereLevelAboveSurface);
                
                bubbleData.transform.localPosition = position;
            }
        }
        
        private void Spawn(ref SpawnProfile profile) {
            var firstPos = ApplyBounds(GetRandomPointInBounds(), profile.excludeCenter);
            SpawnSingle(ref profile, firstPos);
            
            if (Random.Range(0f, 1f) > profile.burstChance) return;
            
            int burstCount = Random.Range(0, profile.maxBurst);
            var up = _transform.up;
            
            for (int i = 0; i < burstCount; i++) {
                var burstPos = firstPos + GetRandomFlatVector(profile.burstDistanceRange, up);
                SpawnSingle(ref profile, ApplyBounds(burstPos, profile.excludeCenter));
            }
        }

        private void SpawnSingle(ref SpawnProfile profile, Vector3 position) {
            var sphere = PrefabPool.Main.Get(_spherePrefab, position, Quaternion.identity, _fleshController.Root);
            var sphereTransform = sphere.transform;

            var data = new BubbleData(
                sphere,
                Time.time,
                profile.lifetimeRange.GetRandomInRange(), 
                profile.startScaleRange.GetRandomInRange(), 
                profile.endScaleRange.GetRandomInRange(),
                profile.startSpeedRangeUp.GetRandomInRange() * Vector3.up + GetRandomFlatVector(profile.startSpeedRangeSide, Vector3.up),
                profile.endSpeedRangeUp.GetRandomInRange() * Vector3.up + GetRandomFlatVector(profile.endSpeedRangeSide, Vector3.up)
            );

            sphereTransform.localScale = data.startScale * Vector3.one;
            sphereTransform.localPosition = sphereTransform.localPosition.WithY(-data.startScale);

            _bubbles.Add(data);
            _deformable.AddDeformer(sphere);
        }

        private Vector3 GetRandomPointInBounds() {
            var bounds = _boxCollider.bounds;
            var rot = _transform.rotation;
            var local = RandomExtensions.GetRandomPointInBox(_boxCollider.size * 0.5f).WithY(0f);
            return bounds.center + rot * local;
        }
        
        private Vector3 ApplyBounds(Vector3 point, Vector2 excludeCenter) {
            var bounds = _boxCollider.bounds;
            var local = _transform.InverseTransformPoint(point) + 
                        _transform.InverseTransformDirection(bounds.center - _transform.position);

            local = RandomExtensions
                    .PlacePointInBounds(local.WithoutY(), _boxCollider.size.WithoutY() * 0.5f, excludeCenter)
                    .ToXZY(local.y);
            
            return bounds.center + _transform.TransformDirection(local);
        }
        
        private static Vector3 GetRandomFlatVector(Vector2 range, Vector3 axis) {
            return range.GetRandomInRange() * RandomExtensions.OnUnitCircle(axis);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected() {
            if (_boxCollider == null) _boxCollider = GetComponent<BoxCollider>();
            
            var rot = transform.rotation;
            var pos = _boxCollider.bounds.center;
            
            for (int i = 0; i < _spawnProfiles?.Length; i++) {
                ref var spawnProfile = ref _spawnProfiles[i];
                if (!spawnProfile.showDebugInfo) continue;
                
                DebugExt.DrawBox(pos, rot, spawnProfile.excludeCenter.ToXZY(1f) * 2f, Color.red, gizmo: true);
            }
        }
#endif
    }
    
}