using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Maths;
using MisterGames.Common.Pooling;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.Runtime.Spider {

    public sealed class SpiderWebLine : MonoBehaviour, IActorComponent {
        
        [SerializeField] private LineRenderer _lineRenderer;
        [SerializeField] private Collider _collider;
        
        [Header("Burn")]
        [SerializeField] private SpiderWebLine _prevNode;
        [SerializeField] private SpiderWebLine _nextNode;
        [SerializeField] private Vector2 _burnTimeRange;
        [SerializeField] private AnimationCurve _dissolveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        
        [Header("Actions")]
        [SerializeReference] [SubclassSelector] private IActorAction _restoreAction;
        [SerializeReference] [SubclassSelector] private IActorAction _burnAction;

        public enum LineState {
            None,
            Restored,
            Burnt,
        }
        
        private static readonly int Dissolve = Shader.PropertyToID("_Dissolve");
        
        public LineRenderer Line => _lineRenderer;
        public LineState State { get; private set; }

        private CancellationTokenSource _cts;
        private ISpiderWebPlacer _spiderWebPlacer;
        private IActor _actor;

        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
        }

        private void OnDestroy() {
            AsyncExt.DisposeCts(ref _cts);
        }

        public void SetPreviousNode(SpiderWebLine node) {
            _prevNode = node;

#if UNITY_EDITOR
            if (!Application.isPlaying) EditorUtility.SetDirty(this);
#endif
        }
        
        public void SetNextNode(SpiderWebLine node) {
            _nextNode = node;
            
#if UNITY_EDITOR
            if (!Application.isPlaying) EditorUtility.SetDirty(this);
#endif
        }

        public void SetBurnTimeRange(Vector2 timeRange) {
            _burnTimeRange = timeRange;
            
#if UNITY_EDITOR
            if (!Application.isPlaying) EditorUtility.SetDirty(this);
#endif
        }
        
        public void Restore(ISpiderWebPlacer spiderWebPlacer, float duration) {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            
            if (State == LineState.Restored) return;

            AsyncExt.RecreateCts(ref _cts);
            if (duration > 0f) RestoreAsync(duration, _cts.Token).Forget();
            
            RestoreSelfAndNeighbours(spiderWebPlacer, spiderWebPlacer.GetMaterial());
        }

        public void Burn(bool notifyBurn = true) {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            
            if (State == LineState.Burnt) return;
            
            var spiderWebPlacer = _spiderWebPlacer;
            
            AsyncExt.RecreateCts(ref _cts);
            BurnAsync(_burnTimeRange.GetRandomInRange(), _cts.Token).Forget();
            
            BurnSelfAndNeighbours();
            
            if (notifyBurn) spiderWebPlacer?.NotifyBurn();
        }

        private void RestoreSelfAndNeighbours(ISpiderWebPlacer spiderWebPlacer, Material material) {
            if (State == LineState.Restored) return;

            State = LineState.Restored;
            _collider.enabled = true;
            _lineRenderer.sharedMaterial = material;
            _spiderWebPlacer = spiderWebPlacer;
            
            if (_prevNode != null) _prevNode.RestoreSelfAndNeighbours(spiderWebPlacer, material);
            if (_nextNode != null) _nextNode.RestoreSelfAndNeighbours(spiderWebPlacer, material);
        }

        private void BurnSelfAndNeighbours() {
            if (State == LineState.Burnt) return;
            
            State = LineState.Burnt;
            _collider.enabled = false;
            _spiderWebPlacer = null;
            
            if (_prevNode != null) _prevNode.BurnSelfAndNeighbours();
            if (_nextNode != null) _nextNode.BurnSelfAndNeighbours();
        }

        private void ReleaseSelfAndNeighbours() {
            if (State == LineState.None) return;
            
            State = LineState.None;
            _collider.enabled = false;
            
            PrefabPool.Main.Release(gameObject);
            
            if (_prevNode != null) _prevNode.ReleaseSelfAndNeighbours();
            if (_nextNode != null) _nextNode.ReleaseSelfAndNeighbours();
        }
        
        private async UniTask RestoreAsync(float duration, CancellationToken cancellationToken) {
            _restoreAction?.Apply(_actor, cancellationToken).Forget();

            await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: cancellationToken)
                .SuppressCancellationThrow();
            
            if (cancellationToken.IsCancellationRequested) return;
            
            AsyncExt.DisposeCts(ref _cts);
        }
        
        private async UniTask BurnAsync(float duration, CancellationToken cancellationToken) {
            _burnAction?.Apply(_actor, cancellationToken).Forget();
            
            float t = 0f;
            float speed = duration > 0f ? 1f / duration : float.MaxValue;
            var material = _lineRenderer.sharedMaterial;
            
            while (!cancellationToken.IsCancellationRequested && t < 1f) {
                t = Mathf.Clamp01(t + Time.deltaTime * speed);
                material.SetFloat(Dissolve, _dissolveCurve.Evaluate(t));
                
                await UniTask.Yield();
            }
            
            if (cancellationToken.IsCancellationRequested) return;
            
            ReleaseSelfAndNeighbours();
        }

        public override string ToString() {
            return $"{nameof(SpiderWebLine)}({State})";
        }

#if UNITY_EDITOR
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void TestBurn() {
            Burn();
        }
#endif
    }
    
}