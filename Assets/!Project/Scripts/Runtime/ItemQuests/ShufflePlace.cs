using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using MisterGames.Actors.Actions;
using MisterGames.Common.Attributes;
using MisterGames.Interact.Interactives;
using UnityEngine;

public sealed class ShufflePlace : Placeable {
    
    [SerializeField] private GameObject[] cars;
    [SerializeField] [Min(0)] private int rightStep = 0;
    [SerializeField] [Min(0)] private int startStep = 0;
    [SerializeField] [Min(0f)] private float _animationTime = 0.3f;

    [SerializeReference] [SubclassSelector] private IActorAction _onMove;
    
    private readonly List<Vector3> positions = new();
    private int _step = 0;
    private Interactive _interactive;
    
    public override bool IsPlacedRight() {
        return _step == rightStep;
    }
    
    private void Awake() {
        _interactive = GetComponent<Interactive>();
        
        for (int i = 0; i < cars.Length; i++) {
            positions.Add(cars[i].transform.localPosition);
        }
        
        SetStep(startStep, instant: true);
    }
    
    private void OnEnable() {
        _interactive.OnStartInteract += OnInteract;
    }

    private void OnDisable() {
        _interactive.OnStartInteract -= OnInteract;
    }

    private void OnInteract(IInteractiveUser user) {
        SetStep(_step - 1, false);
    }

    private void SetStep(int step, bool instant) {
        _step = step < 0 ? cars.Length - 1 : step > cars.Length - 1 ? 0 : step;

        if (!instant) {
            _onMove?.Apply(null, destroyCancellationToken).Forget();
        }

        for (int i = 0; i < cars.Length; i++) {
            var car = cars[i];
            var nextPos = positions[(i + _step) % cars.Length];
            
            if (instant) {
                car.transform.localPosition = nextPos;
                continue;
            }
            
            LMotion.Create(car.transform.localPosition, nextPos, _animationTime).BindToLocalPosition(car.transform);
        }

        OnPlaced();
    }
}
