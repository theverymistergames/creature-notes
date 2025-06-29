using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Attributes;
using MisterGames.Common.Easing;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Interactive))]
public sealed class MultiStateItem : Placeable, IActorComponent, IEventListener {
    
    [Header("Positioning")]
    [SerializeField] [Min(0)] private int startStateId = 0;
    [SerializeField] [Min(0)] private int rightStateId = 1;
    [SerializeField] [Min(0f)] private float animationTime = 1f;
    [SerializeField] private bool useEulerAngles = true;
    [SerializeField] private AnimationCurve _curveIn = EasingType.EaseInSine.ToAnimationCurve();
    [SerializeField] private AnimationCurve _curveMid = EasingType.EaseInOutSine.ToAnimationCurve();
    [SerializeField] private AnimationCurve _curveOut = EasingType.EaseOutSine.ToAnimationCurve();
    [SerializeField] private Vector3[] positions;
    [SerializeField] private Vector3[] rotations;

    [Header("Move Offset")]
    [SerializeField] private Vector3 moveOutVector;
    [SerializeField] [Range(0f, 1f)] private float _moveOutPoint = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float _moveOutPointTime = 0.5f;
    [SerializeField] private MoveOutOverride[] _moveOutOverrides;

    [Header("Reset")]
    [FormerlySerializedAs("levelLoadedEvent")] 
    [SerializeField] private EventReference resetEvent;

    [Header("Actions")]
    [SerializeReference] [SubclassSelector] private IActorAction _onStart;
    [SerializeReference] [SubclassSelector] private IActorAction _onEnd;
    [SerializeField] private StateOverride[] _stateOverrides;
    
    [Serializable]
    private struct StateOverride {
        [Min(0)] public int startState;
        [SerializeReference] [SubclassSelector] public IActorAction onStart;
        [SerializeReference] [SubclassSelector] public IActorAction onEnd;
    }

    [Serializable]
    private struct MoveOutOverride {
        [Min(0)] public int startState;
        public Vector3 moveOutVector;
        [Range(0f, 1f)] public float moveOutPoint;
        [Range(0f, 1f)] public float moveOutPointTime;
    }
    
    private readonly List<(Vector3 point, float progress)> _midPointsBuffer = new();
    private CancellationToken _destroyToken;
    private IActor _actor;
    private Interactive _interactive;
    private int _currentStateId;
    private bool _inTransition;
    private byte _transitionId;

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
        SetStartPosition();
        _currentStateId = startStateId;
        
        _interactive.OnStartInteract += OnStartInteract;
        resetEvent.Subscribe(this);
    }

    private void OnDisable() {
        _interactive.OnStartInteract -= OnStartInteract;
        resetEvent.Unsubscribe(this);
    }

    private void SetStartPosition() {
        if (positions?.Length > 0) transform.localPosition = positions[startStateId];
        if (rotations?.Length > 0) transform.localRotation = Quaternion.Euler(rotations[startStateId]);
    }

    void IEventListener.OnEventRaised(EventReference e) {
        SetStartPosition();
    }

    private void OnStartInteract(IInteractiveUser obj) {
        if (_inTransition) return;
        
        int fromState = _currentStateId;
        int toState = (_currentStateId + 1) % Mathf.Max(positions.Length, rotations.Length);
        
        Transit(transform, fromState, toState, _destroyToken).Forget();
        
        _currentStateId = toState;
        OnPlaced();
    }

    private async UniTask Transit(Transform target, int fromState, int toState, CancellationToken cancellationToken) {
        byte id = ++_transitionId;
        _inTransition = true;

#if UNITY_EDITOR
        if (Application.isPlaying) {
#endif
            
            GetStartAction(_currentStateId)?.Apply(_actor, _destroyToken).Forget();

#if UNITY_EDITOR
        }
#endif
        
        int fromPosState = Mathf.Min(fromState, positions.Length - 1);
        int toPosState = Mathf.Min(toState, positions.Length - 1);
        
        int fromRotState = Mathf.Min(fromState, rotations.Length - 1);
        int toRotState = Mathf.Min(toState, rotations.Length - 1);
        
        var fromPosition = fromPosState >= 0 && fromPosState < positions.Length 
            ? positions[fromPosState] 
            : target.localPosition;
        
        var toPosition = toPosState >= 0 && toPosState < positions.Length 
            ? positions[toPosState] 
            : target.localPosition;
        
        var fromRotation = fromRotState >= 0 && fromRotState < rotations.Length 
            ? rotations[fromRotState] 
            : target.localEulerAngles;
        
        var toRotation = toRotState >= 0 && toRotState < rotations.Length 
            ? rotations[toRotState] 
            : target.localEulerAngles;

        var midPoints = FillMidPoints(fromState, fromPosition, toPosition);

        float t = 0f;
        float speed = animationTime > 0f ? 1f / animationTime : float.MaxValue;

        int midPointIndex = 0;
        int midPointsCount = midPoints.Count;

#if UNITY_EDITOR
        float lastTime = Time.realtimeSinceStartup;
#endif
        
        while (!cancellationToken.IsCancellationRequested && t < 1f && id == _transitionId) {
            float dt = Time.deltaTime;
            
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                dt = Time.realtimeSinceStartup - lastTime;
                lastTime = Time.realtimeSinceStartup;
            }
#endif
            
            t = Mathf.Clamp01(t + speed * dt);
            
            midPointIndex = midPointIndex > midPointsCount - 1 || t < midPoints[midPointIndex].progress 
                ? midPointIndex 
                : midPointIndex + 1; 
            
            (var point0, float p0) = midPointIndex <= 0 ? (fromPosition, 0f) : midPoints[midPointIndex - 1];
            (var point1, float p1) = midPointIndex > midPointsCount - 1 ? (toPosition, 1f) : midPoints[midPointIndex];
            
            float tLocal = p0 < p1 ? Mathf.Clamp01((t - p0) / (p1 - p0)) : 1f;
            var posCurve = p0 <= 0f && p1 < 1f ? _curveIn
                : p1 >= 1f && p0 > 0f ? _curveOut
                : _curveMid;
            
            var pos = Vector3.Lerp(point0, point1, posCurve.Evaluate(tLocal));
            var rot = useEulerAngles 
                ? Quaternion.Euler(Vector3.Lerp(fromRotation, toRotation, _curveMid.Evaluate(t)))
                : Quaternion.Slerp(Quaternion.Euler(fromRotation), Quaternion.Euler(toRotation), _curveMid.Evaluate(t));
            
            target.SetLocalPositionAndRotation(pos, rot);

#if UNITY_EDITOR
            if (!Application.isPlaying) EditorUtility.SetDirty(target);
#endif
            
            await UniTask.Yield();
        }
        
        if (cancellationToken.IsCancellationRequested || id != _transitionId) return;

        _inTransition = false;
        
#if UNITY_EDITOR
        if (Application.isPlaying) {
#endif
            
            GetEndAction(_currentStateId)?.Apply(_actor, cancellationToken).Forget();
        
#if UNITY_EDITOR
        }
#endif
    }

    private IActorAction GetStartAction(int currentStateId) {
        for (int i = 0; i < _stateOverrides.Length; i++) {
            ref var stateOverride = ref _stateOverrides[i];
            if (stateOverride.startState != currentStateId) continue;
            
            return stateOverride.onStart;
        }

        return _onStart;
    }
    
    private IActorAction GetEndAction(int currentStateId) {
        for (int i = 0; i < _stateOverrides.Length; i++) {
            ref var stateOverride = ref _stateOverrides[i];
            if (stateOverride.startState != currentStateId) continue;
            
            return stateOverride.onEnd;
        }

        return _onEnd;
    }
    
    private IReadOnlyList<(Vector3 point, float progress)> FillMidPoints(int fromState, Vector3 fromPosition, Vector3 toPosition) {
        _midPointsBuffer.Clear();
        bool hasOverrides = false;
        
        for (int i = 0; i < _moveOutOverrides?.Length; i++) {
            ref var stateOverride = ref _moveOutOverrides[i];
            if (stateOverride.startState != fromState) continue;

            _midPointsBuffer.Add((
                Vector3.Lerp(fromPosition, toPosition, stateOverride.moveOutPoint) + stateOverride.moveOutVector, 
                stateOverride.moveOutPointTime
            ));
            
            hasOverrides = true;
        }

        if (!hasOverrides) {
            _midPointsBuffer.Add((
                Vector3.Lerp(fromPosition, toPosition, _moveOutPoint) + moveOutVector,
                _moveOutPointTime
            ));
        }
        
        _midPointsBuffer.Sort((a, b) => a.progress.CompareTo(b.progress));
        
        return _midPointsBuffer;
    }

#if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] [Min(0)] private int _testTransitionFrom = 0;
    [SerializeField] [Min(0)] private int _testTransitionTo = 1;
    [SerializeField] [Range(-1, 1)] private int _testStatesDirection = 1;
    
    [Button]
    private void TestTransition() {
        Undo.RecordObject(transform, "MultiStateItem_TestTransition");
        
        Transit(transform, _testTransitionFrom, _testTransitionTo, default).Forget();

        _testTransitionFrom = (_testTransitionFrom + _testStatesDirection) % Mathf.Max(positions?.Length ?? 0, rotations?.Length ?? 0);
        _testTransitionTo = (_testTransitionTo + _testStatesDirection) % Mathf.Max(positions?.Length ?? 0, rotations?.Length ?? 0);
        
        EditorUtility.SetDirty(this);
    }

    [InitializeOnLoad]
    private static class MultiStateItemContextMenuExtensions {

        static MultiStateItemContextMenuExtensions() {
            EditorApplication.contextualPropertyMenu -= OnContextMenuOpening;
            EditorApplication.contextualPropertyMenu += OnContextMenuOpening;
        }

        private static void OnContextMenuOpening(GenericMenu menu, SerializedProperty property) {
            if (property.propertyType != SerializedPropertyType.Vector3 ||
                property.serializedObject.targetObject is not MultiStateItem item ||
                string.IsNullOrWhiteSpace(property.propertyPath) ||
                !property.propertyPath.Contains("positions") && !property.propertyPath.Contains("rotations")) 
            {
                return;
            }

            var propertyCopy = property.Copy();
            bool isPosition = property.propertyPath.Contains("positions");
            
            menu.AddItem(new GUIContent("Write from Transform"), false, () => {
                Undo.RecordObject(item, "MultiStateItem_WriteFromTransform");
                
                propertyCopy.vector3Value = isPosition ? item.transform.localPosition : item.transform.localEulerAngles;

                propertyCopy.serializedObject.ApplyModifiedProperties();
                propertyCopy.serializedObject.Update();
                
                EditorUtility.SetDirty(item);
            });

            menu.AddItem(new GUIContent("Set to Transform"), false, () => {
                Undo.RecordObject(item.transform, "MultiStateItem_SetToTransform");
                
                if (isPosition) item.transform.localPosition = propertyCopy.vector3Value;
                else item.transform.localEulerAngles = propertyCopy.vector3Value;

                EditorUtility.SetDirty(item.transform);
            });
        }
    }
#endif
}
