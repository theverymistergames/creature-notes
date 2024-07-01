using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Core;
using MisterGames.Collisions.Triggers;
using MisterGames.Common.Attributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace MisterGames.Character.Motion {

    public sealed class ActionOnEnableDisable : MonoBehaviour {
        [SerializeReference] [SubclassSelector] private IActorAction _enableAction;
        [SerializeReference] [SubclassSelector] private IActorAction _disableAction;

        private CancellationTokenSource _enableCts;
        private IActor _context;

        private void OnEnable() {
            _enableCts?.Cancel();
            _enableCts?.Dispose();
            _enableCts = new CancellationTokenSource();

            _context = CharacterSystem.Instance.GetCharacter();
            
            _enableAction?.Apply(_context, _enableCts.Token).Forget();
        }

        private void OnDisable() {
            _disableAction?.Apply(_context, _enableCts.Token).Forget();
        }
    }

}
