using System;
using MisterGames.Actors;
using MisterGames.Scenario.Events;
using UnityEngine;

namespace _Project.Scripts.Runtime.Enemies {

    [Serializable]
    public sealed class MonsterDebuffData : IActorData {
        public EventReference debuffEvent;
        public Sprite debuffImage;
    }
    
}