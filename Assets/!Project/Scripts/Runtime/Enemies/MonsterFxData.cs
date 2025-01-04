using MisterGames.Actors;
using System;
using MisterGames.ActionLib.Sounds;

namespace _Project.Scripts.Runtime.Enemies {
    
	[Serializable]
    public sealed class MonsterFxData : IActorData {
        
        public Sound[] sounds;
        
        [Serializable]
        public struct Sound {
            public MonsterEventType eventType;
            public PlaySoundAction soundAction;
        }
    }
    
}