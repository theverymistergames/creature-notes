using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Attributes;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Interactive))]
public sealed class MultiStateItem : Placeable, IActorComponent, IEventListener {
    
    [Header("Positioning")]
    [SerializeField] [Min(0)] private int startStateId = 0;
    [SerializeField] [Min(0)] private int rightStateId = 1;
    [SerializeField] [Min(0f)] private float animationTime = 1f;
    [SerializeField] private Vector3[] positions;
    [SerializeField] private Vector3[] rotations;
    [SerializeField] private Vector3 moveOutVector;
    [SerializeField] [Range(0f, 1f)] private float _moveOutPoint = 0.5f;
    [SerializeField] private MoveOutOverride[] _moveOutOverrides;

    [Header("Reset")]
    [FormerlySerializedAs("levelLoadedEvent")] 
    [SerializeField] private EventReference resetEvent;

    [Header("Actions")]
    [SerializeReference] [SubclassSelector] private IActorAction _onStart;
    [SerializeReference] [SubclassSelector] private IActorAction _onEnd;
    [SerializeField] private StateOverride[] _stateOverrides;
    
    [Header("DEPRECATED, use actions")]
    [ReadOnly] [SerializeField] private AudioClip startSound;
    [ReadOnly] [SerializeField] private AudioClip endSound;

    [Serializable]
    private struct StateOverride {
        [Min(0)] public int startState;
        [SerializeReference] [SubclassSelector] public IActorAction onStart;
    }

    [Serializable]
    private struct MoveOutOverride {
        [Min(0)] public int startState;
        public Vector3 moveOutVector;
        [Range(0f, 1f)] public float moveOutPoint;
    }
    
    private CancellationToken _destroyToken;
    private IActor _actor;
    private Interactive _interactive;
    private MotionHandle _currentTween;
    private int _currentStateId;

    public override bool IsPlacedRight() {
        return _currentStateId == rightStateId;
    }

    void IActorComponent.OnAwake(IActor actor) {
        _actor = actor;
    }

    private void Awake() {
        _destroyToken = destroyCancellationToken;
        _interactive = GetComponent<Interactive>();
    }

    private void OnEnable() {
        _interactive.OnStartInteract += OnStartInteract;
        resetEvent.Subscribe(this);
    }

    private void OnDisable() {
        _interactive.OnStartInteract -= OnStartInteract;
        resetEvent.Unsubscribe(this);
    }

    private void Start() {
        Reset();
        _currentStateId = startStateId;
    }

    private void Reset() {
        if (positions?.Length > 0) {
            transform.localPosition = positions[startStateId];   
        }

        if (rotations?.Length > 0) {
            transform.localEulerAngles = rotations[startStateId];   
        }
    }

    void IEventListener.OnEventRaised(EventReference e) {
        if (e.EventId == resetEvent.EventId) Reset();
    }

    private void OnStartInteract(IInteractiveUser obj) {
        if (_currentTween.IsActive()) return;

        GetStartAction(_currentStateId)?.Apply(_actor, _destroyToken).Forget();
        GetMoveOutVector(_currentStateId, out var moveOutVector, out float moveOutPoint);
        
        int nextId = (_currentStateId + 1) % (positions.Length > 0 ? positions.Length : rotations.Length);
        
        if (positions.Length > 0) {
            var startPosition = positions[_currentStateId];
            var midPosition = Vector3.Lerp(positions[_currentStateId], positions[nextId], moveOutPoint) + moveOutVector;
            var finalPosition = positions[nextId];

            _currentTween = LMotion
                .Create(startPosition, midPosition, animationTime * 0.5f)
                .WithEase(Ease.InSine)
                .WithOnComplete(() => { 
                    _currentTween = LMotion
                        .Create(midPosition, finalPosition, animationTime * 0.5f)
                        .WithEase(Ease.OutSine)
                        .WithOnComplete(() => _onEnd?.Apply(_actor, _destroyToken).Forget())
                        .BindToLocalPosition(transform); 
                })
                .BindToLocalPosition(transform);
        }

        if (rotations.Length > 0) {
            var startRotation = rotations[_currentStateId];
            var finalRotation = rotations[nextId];
            
            _currentTween = LMotion
                .Create(startRotation, finalRotation, animationTime)
                .WithEase(Ease.InOutSine)
                .WithOnComplete(() => {
                    if (positions is not { Length: > 0 }) _onEnd?.Apply(_actor, _destroyToken).Forget();
                })
                .BindToLocalEulerAngles(transform);
        }

        _currentStateId = nextId;

        OnPlaced();
    }

    private IActorAction GetStartAction(int currentStateId) {
        for (int i = 0; i < _stateOverrides.Length; i++) {
            ref var stateOverride = ref _stateOverrides[i];
            if (stateOverride.startState != currentStateId) continue;
            
            return stateOverride.onStart;
        }

        return _onStart;
    }
    
    private void GetMoveOutVector(int currentStateId, out Vector3 moveOutVector, out float moveOutPoint) {
        for (int i = 0; i < _moveOutOverrides.Length; i++) {
            ref var stateOverride = ref _moveOutOverrides[i];
            if (stateOverride.startState != currentStateId) continue;

            moveOutVector = stateOverride.moveOutVector;
            moveOutPoint = stateOverride.moveOutPoint;
            return;
        }

        moveOutVector = this.moveOutVector;
        moveOutPoint = _moveOutPoint;
    }
}
