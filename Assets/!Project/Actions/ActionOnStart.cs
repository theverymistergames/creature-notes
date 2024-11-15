using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Core;
using MisterGames.Common.Attributes;
using UnityEngine;

namespace MisterGames.Character.Motion {

    public sealed class ActionOnStart : MonoBehaviour, IActorComponent {
        
        [SerializeField] private bool _useCharacterAsContext = true;
        [SerializeReference] [SubclassSelector] private IActorAction _action;
        
        private IActor _actor;

        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
        }

        private void Start() {
            _action?.Apply(GetContext(), destroyCancellationToken).Forget();
        }
    
        private IActor GetContext() {
            return _useCharacterAsContext ? CharacterSystem.Instance.GetCharacter() : _actor;
        }
    }
    
}
