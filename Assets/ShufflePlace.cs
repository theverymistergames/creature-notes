using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using LitMotion.Extensions;
using MisterGames.Common.Attributes;
using MisterGames.Interact.Interactives;
using UnityEngine;

public class ShufflePlace : Placeable {
    [SerializeField] private GameObject[] cars;
    [SerializeField] private int rightStep = 0;
    [SerializeField] private int startStep = 0;
    
    private List<Vector3> positions = new List<Vector3>();
    private int _step = 0;
    private Interactive _interactive;
    
    public override bool IsPlacedRight() {
        return _step == rightStep;
    }
    
    private void Awake() {
        foreach (var car in cars) {
            positions.Add(car.transform.localPosition);
        }

        _interactive = GetComponent<Interactive>();
        
        for (var i = 0; i < startStep; i++) {
            Shuffle(true);
        }
    }

    private void ShuffleOnInteract(IInteractiveUser user) {
        Shuffle(false);
    }

    private void Shuffle(bool instant) {
        _step = (_step - 1 < 0 ? cars.Length - 1 : _step - 1);

        var index = 0;
        
        foreach (var car in cars) {
            LMotion.Create(car.transform.localPosition, positions[(index + _step) % cars.Length], instant ? 0.01f : 0.3f).BindToLocalPosition(car.transform);
            index++;
        }
        
        OnPlaced();
    }

    private void OnEnable() {
        _interactive.OnStartInteract += ShuffleOnInteract;
    }

    private void OnDisable() {
        _interactive.OnStartInteract -= ShuffleOnInteract;
    }
}
