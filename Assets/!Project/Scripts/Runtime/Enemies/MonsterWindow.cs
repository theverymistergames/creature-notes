using MisterGames.Actors;
using MisterGames.Common.Maths;
using MisterGames.Tick.Core;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {
    
    public sealed class MonsterWindow : MonoBehaviour, IActorComponent, IUpdate {

        [Header("Window")]
        [SerializeField] private Transform _window;
        [SerializeField] private Vector3 _windowLocalPositionStart;
        [SerializeField] private Vector3 _windowLocalPositionEnd;
        [SerializeField] [Min(0f)] private float _windowSmoothing = 10f;
        [SerializeField] [Range(0f, 1f)] private float _windowMaxProgress = 1f;
        
        [SerializeField] private Transform _monsterVisual;
        [SerializeField] private Vector3 _monsterLocalPositionStart;
        [SerializeField] private Vector3 _monsterLocalPositionEnd;
        [SerializeField] [Min(0f)] private float _monsterSmoothing = 10f;
        [SerializeField] [Range(0f, 1f)] private float _showMonsterAtProgress = 0.8f;
        
        private Monster _monster;
        private GameObject _monsterVisualGo;

        void IActorComponent.OnAwake(IActor actor) {
            _monster = actor.GetComponent<Monster>();
            _monsterVisualGo = _monsterVisual.gameObject;
        }

        private void OnEnable() {
            PlayerLoopStage.Update.Subscribe(this);
        }

        private void OnDisable() {
            PlayerLoopStage.Update.Unsubscribe(this);
        }

        void IUpdate.OnUpdate(float dt) {
            float progress = _monster.Progress;
            
            float windowProgress = Mathf.Clamp01(progress / _windowMaxProgress);
            var windowTargetPos = Vector3.Lerp(_windowLocalPositionStart, _windowLocalPositionEnd, windowProgress);
            _window.localPosition = _window.localPosition.SmoothExpNonZero(windowTargetPos, _windowSmoothing * dt);
            
            var monsterTargetPos = Vector3.Lerp(_monsterLocalPositionStart, _monsterLocalPositionEnd, progress);
            _monsterVisualGo.SetActive(progress >= _showMonsterAtProgress);
            _monsterVisual.localPosition = _monsterVisual.localPosition.SmoothExpNonZero(monsterTargetPos, _monsterSmoothing * dt);
        }
    }
    
}