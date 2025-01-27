using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Common.Colors;
using MisterGames.Common.Pooling;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.Runtime.HUD {
    
    public sealed class MonsterDebuffsUI : MonoBehaviour, IEventListener<Sprite> {

        [SerializeField] private EventReference _debuffImageEvent;
        [SerializeField] private Image _debuffImagePrefab;
        [SerializeField] private Color _startColor = Color.white;
        [SerializeField] [Min(0f)] private float _debuffImageDuration = 1f;
        [SerializeField] private AnimationCurve _alphaCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        private readonly HashSet<Image> _images = new();
        
        private void OnEnable() {
            _debuffImageEvent.Subscribe(this);
        }

        private void OnDisable() {
            _debuffImageEvent.Unsubscribe(this);
        }

        private void OnDestroy() {
            foreach (var image in _images) {
                PrefabPool.Main.Release(image);
            }
            
            _images.Clear();
        }

        void IEventListener<Sprite>.OnEventRaised(EventReference e, Sprite data) {
            SpawnDebuff(data, _debuffImageDuration, destroyCancellationToken).Forget();
        }

        private async UniTask SpawnDebuff(Sprite sprite, float duration, CancellationToken token) {
            var image = PrefabPool.Main.Get(_debuffImagePrefab, transform, worldPositionStays: false);

            _images.Add(image);
            
            image.sprite = sprite;
            image.color = _startColor;
            image.rectTransform.offsetMin = Vector2.zero;
            image.rectTransform.offsetMax = Vector2.zero;
            image.rectTransform.localScale = Vector3.one;
            
            float t = 0f;
            float speed = duration > 0f ? 1f / duration : float.MaxValue;

            while (!token.IsCancellationRequested && t < 1f) {
                t = Mathf.Clamp01(t + Time.unscaledDeltaTime * speed);
                image.color = _startColor.WithAlpha(Mathf.Lerp(_startColor.a, 0f, _alphaCurve.Evaluate(t)));
                
                await UniTask.Yield();
            }
            
            if (token.IsCancellationRequested) return;
            
            PrefabPool.Main.Release(image);
            _images.Remove(image);
        }
    }
    
}