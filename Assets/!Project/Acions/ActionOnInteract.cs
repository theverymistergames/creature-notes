using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Common.Attributes;
using MisterGames.Interact.Interactives;
using UnityEngine;

namespace MisterGames.Character.Motion {

    public sealed class ActionOnInteract : MonoBehaviour {
        
        [SerializeReference] [SubclassSelector] private IActorAction _startAction;
        [SerializeReference] [SubclassSelector] private IActorAction _stopAction;

        private Interactive _interactive;
        private CancellationTokenSource _enableCts;
        
        private void Awake() {
            _interactive = GetComponent<Interactive>();
        }

        private void OnEnable() {
            _enableCts?.Cancel();
            _enableCts?.Dispose();
            _enableCts = new CancellationTokenSource();
            
            _interactive.OnStartInteract += OnStartInteract;
            _interactive.OnStopInteract += OnStopInteract;
        }

        private void OnDisable() {
            _enableCts?.Cancel();
            _enableCts?.Dispose();
            _enableCts = null;
            
            _interactive.OnStartInteract -= OnStartInteract;
            _interactive.OnStopInteract -= OnStopInteract;
        }

        private void OnStartInteract(IInteractiveUser user) {
            if (user.Root.GetComponent<IActor>() is {} actor) _startAction?.Apply(actor, _enableCts.Token).Forget();
        }

        private void OnStopInteract(IInteractiveUser user) {
            if (user.Root.GetComponent<IActor>() is {} actor) _stopAction?.Apply(actor, _enableCts.Token).Forget();
        }
    }

}
