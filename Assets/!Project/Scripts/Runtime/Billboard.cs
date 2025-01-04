using MisterGames.Character.Core;
using MisterGames.Common.Tick;
using UnityEngine;

public sealed class Billboard : MonoBehaviour, IUpdate
{
    private void OnEnable() {
        PlayerLoopStage.LateUpdate.Subscribe(this);
    }

    private void OnDisable() {
        PlayerLoopStage.LateUpdate.Unsubscribe(this);
    }

    public void OnUpdate(float dt) {
        if (CharacterSystem.Instance.GetCharacter() is not {} actor) return;
        
        transform.LookAt(actor.Transform.position, Vector3.up);
    }
}
