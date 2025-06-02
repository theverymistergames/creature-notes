using System;
using LitMotion;
using LitMotion.Extensions;
using MisterGames.Interact.Interactives;
using MisterGames.Scenario.Events;
using UnityEngine;
using UnityEngine.Serialization;

public class Placeable : MonoBehaviour {
    public event Action Placed = delegate { };

    public void OnPlaced() {
        Placed?.Invoke();
    }
    
    public virtual bool IsPlacedRight() {
        return false;
    }
}
