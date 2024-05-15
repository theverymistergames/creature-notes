using System.Threading;
using Cysharp.Threading.Tasks;
using MisterGames.Actors;
using MisterGames.Actors.Actions;
using MisterGames.Tick.Core;
using UnityEngine;

public class MagicCaster : MonoBehaviour, IActorComponent {
    
    [SerializeField] private float _strikeSpeed = 0.1f;
    [SerializeField] private GameObject StrikeContainerPrefab;
    [SerializeField] private ActorAction _onFireAction;

    private IActor _actor;
    private CancellationTokenSource _enableCts;

    void IActorComponent.OnAwake(IActor actor) {
        _actor = actor;
    }

    private void OnEnable() {
        _enableCts?.Cancel();
        _enableCts?.Dispose();
        _enableCts = new CancellationTokenSource();
    }

    private void OnDisable() {
        _enableCts?.Cancel();
        _enableCts?.Dispose();
        _enableCts = null;
    }

    private async UniTask StrikeRunes(CancellationToken cancellationToken) {
        _onFireAction.Apply(_actor, cancellationToken).Forget();

        var container = Instantiate(StrikeContainerPrefab, gameObject.transform);
        container.transform.localPosition = new Vector3(0, 0, 0.1f);
        container.transform.SetParent(null);

        var strikeContainer = container.GetComponent<ProjectileContainer>();
        var timeSource = TimeSources.Get(PlayerLoopStage.Update);
        var currentDirection = (container.transform.position - gameObject.transform.position).normalized;
        float timer = 0f;
        
        while (!cancellationToken.IsCancellationRequested && timer < 5f) {
            float dt = timeSource.DeltaTime;
            
            timer += dt;
            container.transform.position += currentDirection * _strikeSpeed * dt;
            
            if (strikeContainer.collided) break;
            
            await UniTask.Yield();
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) {
            StrikeRunes(_enableCts.Token).Forget();
        }
    }
}
