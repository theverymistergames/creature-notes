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
        [SerializeReference] [SubclassSelector] private IActorAction _burnAction;
        
        private static readonly int Dissolve = Shader.PropertyToID("_Dissolve");
        
        public LineRenderer Line => _lineRenderer;

        private CancellationTokenSource _enableCts;
        private IActor _actor;
        private bool _isBurnt;

        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);
            
            Restore();
        }

        private void OnDisable() { 
            AsyncExt.DisposeCts(ref _enableCts);
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

        public void Restore() {
#if UNITY_EDITOR
            if (!Application.isPlaying) return;
#endif
            
            _isBurnt = false;
            _collider.enabled = true;
            
            _lineRenderer.material.SetFloat(Dissolve, 0f);
        }

        public void Burn() {
            if (_isBurnt) return;

            BurnSelfAndNeighbours();
        }

        private void BurnSelfAndNeighbours() {
            if (_isBurnt) return;
            _isBurnt = true;
            
            _collider.enabled = false;
            
            if (_prevNode != null) _prevNode.BurnSelfAndNeighbours();
            if (_nextNode != null) _nextNode.BurnSelfAndNeighbours();
            
            BurnAsync(_burnTimeRange.GetRandomInRange(), _enableCts.Token).Forget();
        }

        private async UniTask BurnAsync(float duration, CancellationToken cancellationToken) {
            _burnAction?.Apply(_actor, cancellationToken).Forget();
            
            float t = 0f;
            float speed = duration > 0f ? 1f / duration : float.MaxValue;

            while (!cancellationToken.IsCancellationRequested && t < 1f) {
                t = Mathf.Clamp01(t + Time.deltaTime * speed);
                
                _lineRenderer.material.SetFloat(Dissolve, _dissolveCurve.Evaluate(t));
                
                await UniTask.Yield();
            }
            
            if (cancellationToken.IsCancellationRequested) return;
            
            PrefabPool.Main.Release(gameObject);
        }

#if UNITY_EDITOR
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void TestBurn() {
            Burn();
        }
#endif
    }
    
}