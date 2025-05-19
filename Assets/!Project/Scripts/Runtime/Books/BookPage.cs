using System;
using System.Collections.Generic;
using MisterGames.Common.Attributes;
using UnityEngine;

namespace _Project.Scripts.Runtime.Books {
    
    public sealed class BookPage : MonoBehaviour {
        
        [SerializeField] private Animator _animator;
        [SerializeField] private Renderer _renderer;
        [SerializeField] [Min(0f)] private float _pageAnimationDuration = 2.042f;
        
        private static readonly int FlipForward = Animator.StringToHash("PageAnim");
        private static readonly int FlipBack = Animator.StringToHash("PageAnim_Reversed");

        public float FlipDuration => _pageAnimationDuration;
        private readonly List<Material> _materials = new();
        private bool _isOpened;
        
        private void Awake() {
            _materials.Add(_renderer.sharedMaterial);
            _materials.Add(null);
        }

        private void OnEnable() {
            SetOpened(_isOpened);
        }

        public void SetOpened(bool isOpened, float speed = 0f) {
            _isOpened = isOpened;
            
            if (!gameObject.activeSelf) return;
            
            int nextState = isOpened ? FlipForward : FlipBack;
            var data = _animator.GetCurrentAnimatorStateInfo(0);

            if (speed > 0f && data.shortNameHash == nextState) {
                _animator.speed = speed;
                return;
            }

            float fixedTimeOffset = speed > 0f 
                ? (1f - Mathf.Clamp01(data.normalizedTime)) * data.length 
                : _pageAnimationDuration;

            _animator.speed = speed > 0f ? speed : 1f;
            _animator.CrossFadeInFixedTime(nextState, fixedTransitionDuration: 0f, 0, fixedTimeOffset, normalizedTransitionTime: 0f);
        }

        public void SetMaterial(Material material) {
            _materials[1] = material;
            _renderer.SetSharedMaterials(_materials);
        }

#if UNITY_EDITOR
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void FlipForwardNormal() => SetOpened(isOpened: true, speed: 1f);
        
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void FlipBackwardNormal() => SetOpened(isOpened: false, speed: 1f);
        
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void FlipForwardInstant() => SetOpened(isOpened: true, speed: -1f);
        
        [Button(mode: ButtonAttribute.Mode.Runtime)]
        private void FlipBackwardInstant() => SetOpened(isOpened: false, speed: -1f);
#endif
    }
    
}