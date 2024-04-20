using System.Threading;
using Cysharp.Threading.Tasks;
using DigitalRuby.Tween;
using MisterGames.Character.Actions;
using MisterGames.Character.Core;
using UnityEngine;

public class MagicCaster : MonoBehaviour {
    
    [SerializeField] private float _strikeSpeed = 0.1f;
    [SerializeField] private GameObject StrikeContainerPrefab;

    [SerializeField] private CharacterAccess _characterAccess;
    [SerializeField] private CharacterActionAsset _onFireAction;

    private CancellationTokenSource _enableCts;

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

    private void Start() {
        TweenFactory.ClearTweensOnLevelLoad = true;
        TweenFactory.Clear();
    }

    private void StrikeRunes() {
        _onFireAction.Apply(_characterAccess, _enableCts.Token).Forget();

        var container = Instantiate(StrikeContainerPrefab, gameObject.transform);
        container.transform.localPosition = new Vector3(0, 0, 0.1f);
        container.transform.SetParent(null);

        var strikeContainer = container.GetComponent<ProjectileContainer>();
        
        var currentDirection = (container.transform.position - gameObject.transform.position).normalized;
        
        var tween = TweenFactory.Tween(null, 0, 1, 5, TweenScaleFunctions.QuadraticEaseIn,
            (t) => {
                container.transform.position += currentDirection * _strikeSpeed;
                if (strikeContainer.collided) t.Stop(TweenStopBehavior.DoNotModify);
            });
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0)) {
            StrikeRunes();
        }
    }
}
