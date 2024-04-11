using System;
using LitMotion;
using LitMotion.Extensions;
using MisterGames.Interact.Interactives;
using UnityEngine;
using UnityEngine.Events;

public class MultiStateItem : MonoBehaviour {
    [SerializeField] private int startStateId = 0;
    [SerializeField] private int rightStateId = 1;
    [SerializeField] private Vector3 moveOutVector = new Vector3();
    [SerializeField] private Vector3[] positions;
    [SerializeField] private Vector3[] rotations;
    [SerializeField] private float animationTime = 1f;
    
    private Interactive _interactive;
    private int _currentStateId;
    private MotionHandle _currentTween;

    [NonSerialized] public readonly UnityEvent Placed = new();

    public bool IsPlacedRight() {
        return _currentStateId == rightStateId;
    }

    private void Start() {
        _interactive = GetComponent<Interactive>();
        _interactive.OnStartInteract += InteractiveOnOnStartInteract;

        if (positions.Length > 0) {
            transform.position = positions[startStateId];   
        }

        if (rotations.Length > 0) {
            transform.localEulerAngles = rotations[startStateId];   
        }
        
        _currentStateId = startStateId;
    }

    private void InteractiveOnOnStartInteract(IInteractiveUser obj) {
        var nextId = (_currentStateId + 1) % (positions.Length > 0 ? positions.Length : rotations.Length);

        if (_currentTween.IsActive()) {
            return;
        }
        
        if (positions.Length > 0) {
            var startPosition = positions[_currentStateId];
            var midPosition = positions[_currentStateId] + (positions[nextId] - positions[_currentStateId]) / 2 + moveOutVector;
            var finalPosition = positions[nextId];

            _currentTween = LMotion.Create(startPosition, midPosition, animationTime / 2).WithEase(Ease.InSine).WithOnComplete(() => {
                _currentTween = LMotion.Create(midPosition, finalPosition, animationTime / 2).WithEase(Ease.OutSine).BindToLocalPosition(transform);
            }).BindToLocalPosition(transform);
        }

        if (rotations.Length > 0) {
            var startRotation = rotations[_currentStateId];
            var finalRotation = rotations[nextId];
            
            _currentTween = LMotion.Create(startRotation, finalRotation, animationTime).WithEase(Ease.InOutSine).BindToLocalEulerAngles(transform);
        }

        _currentStateId = nextId;
        
        Placed.Invoke();
    }
}
