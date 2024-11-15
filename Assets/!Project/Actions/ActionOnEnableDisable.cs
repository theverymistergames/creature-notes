using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Core;
using MisterGames.Common.Async;
using MisterGames.Common.Attributes;
using UnityEngine;

namespace MisterGames.Character.Motion {

    public sealed class ActionOnEnableDisable : MonoBehaviour, IActorComponent {
        
        [SerializeField] private bool _useCharacterAsContext = true;
        [SerializeField] private bool _cancelOnNextAction;
        [SerializeReference] [SubclassSelector] private IActorAction _enableAction;
        [SerializeReference] [SubclassSelector] private IActorAction _disableAction;

        private CancellationTokenSource _actionCts;
        private IActor _actor;

        void IActorComponent.OnAwake(IActor actor) {
            _actor = actor;
        }

        private void OnEnable() {
            var token = destroyCancellationToken;
            
            if (_cancelOnNextAction) {
                AsyncExt.RecreateCts(ref _actionCts);
                token = CancellationTokenSource.CreateLinkedTokenSource(token, _actionCts.Token).Token;
            }
            
            _enableAction?.Apply(GetContext(), token).Forget();
        }

        private void OnDisable() {
            var token = destroyCancellationToken;
            
            if (_cancelOnNextAction) {
                AsyncExt.RecreateCts(ref _actionCts);
                token = CancellationTokenSource.CreateLinkedTokenSource(token, _actionCts.Token).Token;
            }
            
            _disableAction?.Apply(GetContext(), token).Forget();
        }

        private IActor GetContext() {
            return _useCharacterAsContext ? CharacterSystem.Instance.GetCharacter() : _actor;
        }
    }

}
