using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors.Actions;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using MisterGames.Common.Pooling;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Books {

    public sealed class Book : MonoBehaviour {

        [SerializeField] private Material[] _pageMaterials;
        [SerializeField] [Min(0f)] private float _pageFlipSpeed = 2f;
        [SerializeField] [Range(0f, 1f)] private float _waitPage = 1f;
        [SerializeField] private Transform _pagesRoot;
        [SerializeField] private BookPage _pagePrefab;
        [SerializeField] private Interactive _leftPageInteractive;
        [SerializeField] private Interactive _rightPageInteractive;
        [SerializeField] private EventReference _pageFlipEvent;
        [SerializeReference] [SubclassSelector] private IActorAction _onFlipAction;

        private BookPage[] _pages;

        private CancellationTokenSource _enableCts;
        private int _currentPage;
        private int _maxReachedPage;
        private bool _interactive;
        private bool _isWaitingPage;

        private void Awake() {
            _pages = new BookPage[Math.Min(3, _pageMaterials.Length)];
            
            for (int i = 0; i < _pages.Length; i++) {
                var page = PrefabPool.Main.Get(_pagePrefab, _pagesRoot);
                page.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                
                _pages[i] = page;
            }
        }

        private void OnEnable() {
            AsyncExt.RecreateCts(ref _enableCts);

            _leftPageInteractive.OnStartInteract += MoveLeft;
            _rightPageInteractive.OnStartInteract += MoveRight;
            
            for (int i = 0; i < _pages.Length; i++) {
                var page = _pages[i];
                page.SetMaterial(_pageMaterials[i]);
                page.SetOpened(isOpened: false, speed: -1f);
            }
            
            _currentPage = 0;
            _maxReachedPage = 0;
            _isWaitingPage = false;
        }

        private void OnDisable() {
            AsyncExt.DisposeCts(ref _enableCts);

            _leftPageInteractive.OnStartInteract -= MoveLeft;
            _rightPageInteractive.OnStartInteract -= MoveRight;

            _isWaitingPage = false;
        }

        public void SetInteractive(bool interactive) {
            _interactive = interactive;
            _leftPageInteractive.enabled = interactive && _currentPage > 0;
            _rightPageInteractive.enabled = interactive && _currentPage < _pageMaterials.Length - 1;
        }

        private void MoveRight(IInteractiveUser obj) {
            FlipPage(1, _enableCts.Token).Forget();
            SetInteractive(_interactive);
        }

        private void MoveLeft(IInteractiveUser obj) {
            FlipPage(-1, _enableCts.Token).Forget();
            SetInteractive(_interactive);
        }

        private async UniTask FlipPage(int dir, CancellationToken cancellationToken) {
            int next = Mathf.Clamp(_currentPage + dir, 0, _pageMaterials.Length - 1);
            if (next == _currentPage || _isWaitingPage) return;
            
            _isWaitingPage = true;
            
            int prev = _currentPage - dir;
            
            var prevPage = prev >= 0 ? _pages[prev % _pages.Length] : null;
            var page = _pages[_currentPage % _pages.Length];
            var nextPage = _pages[next % _pages.Length];

            _maxReachedPage = Mathf.Max(_maxReachedPage, _currentPage);
            if (next > _maxReachedPage) _pageFlipEvent.Raise();
            
            _currentPage = next;

            float startTime = Time.time;

            _onFlipAction?.Apply(null, cancellationToken).Forget();
            
            if (dir > 0) {
                page.SetOpened(isOpened: true, _pageFlipSpeed);
                
                nextPage.SetOpened(isOpened: false);
                nextPage.SetMaterial(_pageMaterials[next]);
                
                await UniTask.Yield();
                if (cancellationToken.IsCancellationRequested) return;
                
                nextPage.gameObject.SetActive(true);
            }
            else {
                if (prevPage != null) prevPage.gameObject.SetActive(false);
                prevPage = page;
                
                nextPage.SetOpened(isOpened: false, speed: _pageFlipSpeed);
                
                if (next + dir >= 0 && next + dir < _pageMaterials.Length) {
                    var nextNextPage = _pages[(next + dir) % _pages.Length];
                    nextNextPage.SetOpened(true);
                    nextNextPage.SetMaterial(_pageMaterials[next + dir]);
                    
                    await UniTask.Yield();
                    if (cancellationToken.IsCancellationRequested) return;
                    
                    nextNextPage.gameObject.SetActive(true);
                }
            }
            
            float timeGone = Time.time - startTime;
            
            await UniTask.Delay(TimeSpan.FromSeconds(_waitPage * page.FlipDuration / _pageFlipSpeed - timeGone), cancellationToken: cancellationToken)
                .SuppressCancellationThrow();
            
            if (cancellationToken.IsCancellationRequested) return;
            
            if (prevPage != null) prevPage.gameObject.SetActive(false);
            
            _isWaitingPage = false;
        }
    }
    
}