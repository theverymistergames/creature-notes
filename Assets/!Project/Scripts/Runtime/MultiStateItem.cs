using System;
using LitMotion;
using LitMotion.Extensions;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;

public class MultiStateItem : Placeable, IEventListener {
    
    [SerializeField] private int startStateId = 0;
    [SerializeField] private int rightStateId = 1;
    [SerializeField] private Vector3 moveOutVector = new Vector3();
    [SerializeField] private Vector3[] positions;
    [SerializeField] private Vector3[] rotations;
    [SerializeField] private float animationTime = 1f;

    [FormerlySerializedAs("levelLoadedEvent")] [SerializeField] private EventReference resetEvent;
    
    // public event Action Placed = delegate { };
    
    private Interactive _interactive;
    private int _currentStateId;
    private MotionHandle _currentTween;

    public override bool IsPlacedRight() {
        return _currentStateId == rightStateId;
    }

    private void Awake() {
        _interactive = GetComponent<Interactive>();
    }

    private void OnEnable() {
        _interactive.OnStartInteract += InteractiveOnOnStartInteract;
        resetEvent.Subscribe(this);
    }

    private void OnDisable() {
        _interactive.OnStartInteract -= InteractiveOnOnStartInteract;
        resetEvent.Unsubscribe(this);
    }

    private void Start() {
        Reset();
        _currentStateId = startStateId;
    }

    private void Reset() {
        if (positions.Length > 0) {
            transform.localPosition = positions[startStateId];   
        }

        if (rotations.Length > 0) {
            transform.localEulerAngles = rotations[startStateId];   
        }
    }

    void IEventListener.OnEventRaised(EventReference e) {
        if (e.EventId == resetEvent.EventId) Reset();
    }

    private void InteractiveOnOnStartInteract(IInteractiveUser obj) {
        if (_currentTween.IsActive()) return;
        
        var nextId = (_currentStateId + 1) % (positions.Length > 0 ? positions.Length : rotations.Length);
        
        if (positions.Length > 0) {
            var startPosition = positions[_currentStateId];
            var midPosition = positions[_currentStateId] + (positions[nextId] - positions[_currentStateId]) / 2 + moveOutVector;
            var finalPosition = positions[nextId];

            _currentTween = LMotion
                .Create(startPosition, midPosition, animationTime / 2)
                .WithEase(Ease.InSine)
                .WithOnComplete(() => { 
                    _currentTween = LMotion
                        .Create(midPosition, finalPosition, animationTime / 2)
                        .WithEase(Ease.OutSine)
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
                .BindToLocalEulerAngles(transform);
        }

        _currentStateId = nextId;

        OnPlaced();
    }
}
