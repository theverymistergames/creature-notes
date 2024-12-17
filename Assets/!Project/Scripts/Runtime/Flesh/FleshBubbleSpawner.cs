using System;
using System.Collections.Generic;
using Deform;
using MisterGames.Common;
using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Common.Pooling;
using MisterGames.Tick.Core;
using UnityEngine;
using UnityEngine.VFX;
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
            public Vector3 excludeCenter;
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
        
        private static readonly int RadiusId = Shader.PropertyToID("Radius"); 

        private readonly List<BubbleData> _bubbles = new();
        private readonly HashSet<Transform> _explosions = new();
        private float[] _spawnTimes;
        private BoxCollider _boxCollider;
        
        private void Awake() {
            _boxCollider = GetComponent<BoxCollider>();
            _boxCollider.isTrigger = true;
            
            _spawnTimes = new float[_spawnProfiles.Length];
        }

        private void OnDestroy() {
            for (int i = 0; i < _bubbles.Count; i++) {
                var bubbleData = _bubbles[i];
                _deformable.RemoveDeformer(bubbleData.deformer);
                PrefabPool.Main.Release(bubbleData.transform);
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
            float progress = _fleshController.GetProgress();
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
                        var vfx = PrefabPool.Main.Get<VisualEffect>(_sphereExplosionVfx, bubbleData.transform.position, Quaternion.identity);
                        vfx.SetFloat(RadiusId, bubbleData.endScale);
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
            var position = GetRandomSpawnPosition(profile.excludeCenter);
            SpawnSingle(ref profile, position);
            
            if (Random.Range(0f, 1f) > profile.burstChance) return;
            
            int burstCount = Random.Range(0, profile.maxBurst);
            
            for (int i = 0; i < burstCount; i++) {
                var spread = Random.insideUnitCircle * profile.burstDistanceRange.GetRandomInRange();
                var pos = position + new Vector3(spread.x, 0f, spread.y);

                SpawnSingle(ref profile, pos);
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
                profile.startSpeedRangeUp.GetRandomInRange() * Vector3.up + GetRandomFlatVector(profile.startSpeedRangeSide),
                profile.endSpeedRangeUp.GetRandomInRange() * Vector3.up + GetRandomFlatVector(profile.endSpeedRangeSide)
            );

            sphereTransform.localScale = data.startScale * Vector3.one;
            sphereTransform.localPosition = sphereTransform.localPosition.WithY(-data.startScale);

            _bubbles.Add(data);
            _deformable.AddDeformer(sphere);
        }

        private Vector3 GetRandomSpawnPosition(Vector3 excludeCenter) {
            var bounds = _boxCollider.bounds;
            var box = _boxCollider.transform;
            
            var local = new Vector3(
                GetRandomExcluded(bounds.extents.x, excludeCenter.x), 
                GetRandomExcluded(bounds.extents.y, excludeCenter.y), 
                GetRandomExcluded(bounds.extents.z, excludeCenter.z) 
            );

            return (bounds.center + box.TransformDirection(local)).WithY(box.position.y);
        }

        private static float GetRandomExcluded(float range, float exclude) {
            range = Mathf.Abs(range);
            exclude = Mathf.Min(Mathf.Abs(exclude), range);
            return Random.Range(exclude, range) * (Random.Range(0, 2) * 2 - 1);
        }

        private static Vector3 GetRandomFlatVector(Vector2 range) {
            var dir = Random.insideUnitCircle;
            return range.GetRandomInRange() * new Vector3(dir.x, 0, dir.y);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            var pos = transform.position;
            var rot = transform.rotation;
            
            for (int i = 0; i < _spawnProfiles?.Length; i++) {
                ref var spawnProfile = ref _spawnProfiles[i];
                if (!spawnProfile.showDebugInfo) continue;
                
                DebugExt.DrawBox(pos, rot, spawnProfile.excludeCenter, Color.red, gizmo: true);
            }
        }
#endif
    }
    
}