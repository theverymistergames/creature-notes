using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Data;

namespace _Project.Scripts.Runtime.Enemies.Lamp {
    
    [Serializable]
    public sealed class SetAffectLampMonsterAction : IActorAction {
        
        public LampMonsterArmBehaviour lampMonsterArmBehaviour;
        public Optional<bool> affectVisibility;
        public Optional<bool> affectScale;
        
        public UniTask Apply(IActor context, CancellationToken cancellationToken = default) {
            if (affectVisibility.HasValue) lampMonsterArmBehaviour.AffectMonsterVisibility(affectVisibility.Value);
            if (affectScale.HasValue) lampMonsterArmBehaviour.AffectMonsterScale(affectScale.Value);
            
            return default;
        }
    }
    
}