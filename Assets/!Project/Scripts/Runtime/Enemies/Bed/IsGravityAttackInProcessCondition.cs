using System;
using MisterGames.Actors;
using MisterGames.Actors.Actions;

namespace _Project.Scripts.Runtime.Enemies.Bed {
    
    [Serializable]
    public sealed class IsGravityAttackInProcessCondition : IActorCondition {

        public bool shouldBeInProcess;
        
        public bool IsMatch(IActor context, float startTime) {
            return context.TryGetComponent(out GravityAttackBehaviour gravityAttackBehaviour) && 
                   gravityAttackBehaviour.IsAttackInProcess == shouldBeInProcess;
        }
    }
    
}