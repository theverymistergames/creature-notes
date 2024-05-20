using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Character.Core;
using MisterGames.Collisions.Triggers;
using MisterGames.Common.Attributes;
using MisterGames.Interact.Interactives;
using UnityEngine;
using UnityEngine.Serialization;

namespace MisterGames.Character.Motion {

    public sealed class ActionOnInteract : MonoBehaviour {
        [SerializeReference] [SubclassSelector] private IActorAction _action;

        private Interactive _interactive;
        private CancellationTokenSource _enableCts;
        private IActor _context;

        private void OnEnable() {
            _interactive = GetComponent<Interactive>();
            _interactive.OnStartInteract += InteractiveOnOnStartInteract;
        }

        private void InteractiveOnOnStartInteract(IInteractiveUser obj) {
            _enableCts?.Cancel();
            _enableCts?.Dispose();
            _enableCts = new CancellationTokenSource();
            _context = CharacterAccessRegistry.Instance.GetCharacterAccess();
            
            _action?.Apply(_context, _enableCts.Token).Forget();
        }

        private void OnDisable() {
            _interactive.OnStartInteract -= InteractiveOnOnStartInteract;
        }
    }

}
