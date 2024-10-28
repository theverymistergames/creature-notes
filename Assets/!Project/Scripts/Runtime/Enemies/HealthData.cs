using System;
using MisterGames.Actors;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {

    [Serializable]
    public sealed class HealthData : IActorData {
        [Min(0f)] public float health;
    }
    
}