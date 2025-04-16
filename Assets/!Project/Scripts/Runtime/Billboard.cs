using MisterGames.Actors;
using MisterGames.Character.Core;
using MisterGames.Common.Tick;
using UnityEngine;

public sealed class Billboard : MonoBehaviour, IUpdate
{
    private IActor _actor;
    private Transform _camera;
    
    private void OnEnable() {
        PlayerLoopStage.LateUpdate.Subscribe(this);
    }

    private void OnDisable() {
        PlayerLoopStage.LateUpdate.Unsubscribe(this);
        
        _actor = null;
        _camera = null;
    }

    public void OnUpdate(float dt) {
        if (CharacterSystem.Instance.GetCharacter() is not { } actor) {
            _actor = null;
            _camera = null;
            return;
        }

        if (_actor == null || _actor != actor) {
            _actor = actor;
            _camera = actor.GetComponent<Camera>().transform;
        }
        
        transform.LookAt(_camera.position, Vector3.up);
    }
}
