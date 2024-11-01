using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Core;
using MisterGames.Common.Attributes;
using UnityEngine;

namespace MisterGames.Character.Motion {

    public sealed class ActionOnStart : MonoBehaviour {
        [SerializeReference] [SubclassSelector]
        private IActorAction _action;

        private CancellationTokenSource _cts;
        private IActor _context;

        private void Start() {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            _context = CharacterSystem.Instance.GetCharacter();
            _action?.Apply(_context, _cts.Token).Forget();
        }
    }
}
