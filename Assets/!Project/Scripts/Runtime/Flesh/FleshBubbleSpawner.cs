using System;
using System.Collections.Generic;
using System.Threading;
using MisterGames.ActionLib.Sounds;
using MisterGames.Common;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Common.Pooling;
using MisterGames.Common.Tick;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Runtime.Flesh {
    
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FleshBubbleSpawner : MonoBehaviour, IUpdate {

        [SerializeField] private FleshController _fleshController;
        [SerializeField] private FleshVertexPosition _fleshVertexPosition;
        
        [Header("Spawn Settings")]
        [SerializeField] private GameObject _sphereExplosionVfx;
        [SerializeField] private PlaySoundAction _soundAction;
        [SerializeField] private SpawnProfile[] _spawnProfiles;
        
        [Serializable]
        private struct SpawnProfile {
            
            [Header("Spawn")]
            [MinMaxSlider(0f, 1f)] public Vector2 progressRange;
            public AnimationCurve progressCurve;
            [FormerlySerializedAs("spawnDelayRange")] [MinMaxSlider(0f, 10f)] public Vector2 spawnDelayRange0;
            [MinMaxSlider(0f, 10f)] public Vector2 spawnDelayRange1;
            [FormerlySerializedAs("lifetimeRange")] [MinMaxSlider(0f, 10f)] public Vector2 lifetimeRange0;
            [MinMaxSlider(0f, 10f)] public Vector2 lifetimeRange1;
            
            [Header("Burst")]
            [FormerlySerializedAs("burstChance")] [Range(0f, 1f)] public float burstChance0;
            [Range(0f, 1f)] public float burstChance1;
            [FormerlySerializedAs("maxBurst")] [Min(0)] public int maxBurst0;
            [Min(0)] public int maxBurst1;
            [MinMaxSlider(0f, 10f)] public Vector2 burstDistanceRange;
            
            [Header("Positioning")]
            public Vector2 excludeCenter;
            [MinMaxSlider(0f, 2f)] public Vector2 endScaleRange;

            [Header("Debug")]
            public bool showDebugInfo;
        }

        private readonly struct BubbleData {
            
            public readonly float lifetime;
            public readonly float scale;
            public readonly Vector3 position;
            
            public BubbleData(float lifetime, Vector3 position, float scale) 
            {
                this.lifetime = lifetime;
                this.position = position;
                this.scale = scale;
            }
        }

        private CancellationTokenSource _enableCts;
        private readonly List<BubbleData> _bubbles = new();
        private float[] _spawnTimers;
        private BoxCollider _boxCollider;
        private Transform _transform;
        
        private void Awake() {
            _transform = transform;
            
            _boxCollider = GetComponent<BoxCollider>();
            _boxCollider.isTrigger = true;
            
            _spawnTimers = new float[_spawnProfiles.Length];
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            PlayerLoopStage.Update.Subscribe(this);
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);
            PlayerLoopStage.Update.Unsubscribe(this);
        }

        void IUpdate.OnUpdate(float dt) {
            ProcessSpawns(dt);
            ProcessBubbles(dt);
        }

        private void ProcessSpawns(float dt) {
            float progress = _fleshController.Progress;
            if (progress <= 0f) return;
            
            for (int i = 0; i < _spawnProfiles?.Length; i++) {
                ref var spawnProfile = ref _spawnProfiles[i];
                ref float spawnTimer = ref _spawnTimers[i];

                spawnTimer -= dt;
                
                if (spawnTimer > 0f || !progress.InRange(spawnProfile.progressRange)) continue;

                float p = spawnProfile.progressCurve.Evaluate(progress.Map01(spawnProfile.progressRange));
                spawnTimer = Mathf.Lerp(spawnProfile.spawnDelayRange0.GetRandomInRange(), spawnProfile.spawnDelayRange1.GetRandomInRange(), p);
                
                Spawn(ref spawnProfile, p);
            }
        }

        private void ProcessBubbles(float dt) {
            for (int i = _bubbles.Count - 1; i >= 0; i--) {
                var bubbleData = _bubbles[i];
                bubbleData = new BubbleData(bubbleData.lifetime - dt, bubbleData.position, bubbleData.scale);
                _bubbles[i] = bubbleData;
                
                if (bubbleData.lifetime > 0f) continue;

                var pos = bubbleData.position;
                _fleshVertexPosition.TrySamplePosition(ref pos);
                
                var vfx = PrefabPool.Main.Get(_sphereExplosionVfx, pos, Quaternion.identity).transform;
                vfx.localScale *= bubbleData.scale;
                
                _soundAction.position = PlaySoundAction.PositionMode.ExplicitTransform;
                _soundAction.transform = vfx;
                _soundAction.Apply(null, _enableCts.Token);
                
                _bubbles.RemoveAt(i);
            }
        }
        
        private void Spawn(ref SpawnProfile profile, float progress) {
            var firstPos = ApplyBounds(GetRandomPointInBounds(), profile.excludeCenter);
            SpawnSingle(ref profile, firstPos, progress);
            
            float burstChance = Mathf.Lerp(profile.burstChance0, profile.burstChance1, progress);
            if (Random.value >= burstChance) return;
            
            int burstCount = Random.Range(0, Mathf.CeilToInt(Mathf.Lerp(profile.maxBurst0, profile.maxBurst1, progress)));
            var up = _transform.up;
            
            for (int i = 0; i < burstCount; i++) {
                var burstPos = firstPos + GetRandomFlatVector(profile.burstDistanceRange, up);
                SpawnSingle(ref profile, ApplyBounds(burstPos, profile.excludeCenter), progress);
            }
        }

        private void SpawnSingle(ref SpawnProfile profile, Vector3 position, float progress) {
            _bubbles.Add(new BubbleData(
                lifetime: Mathf.Lerp(profile.lifetimeRange0.GetRandomInRange(), profile.lifetimeRange1.GetRandomInRange(), progress),
                position,
                scale: profile.endScaleRange.GetRandomInRange()
            ));
        }

        private Vector3 GetRandomPointInBounds() {
            var local = RandomExtensions.GetRandomPointInBox(_boxCollider.size.Multiply(_transform.localScale) * 0.5f).WithY(0f);
            return _boxCollider.bounds.center + _transform.rotation * local;
        }
        
        private Vector3 ApplyBounds(Vector3 point, Vector2 excludeCenter) {
            var bounds = _boxCollider.bounds;
            var local = _transform.InverseTransformPoint(point) - 
                        _transform.InverseTransformPoint(bounds.center);

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