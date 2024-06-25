using MisterGames.Character.Core;
using MisterGames.Tick.Core;
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
        if (CharacterAccessRegistry.Instance.GetCharacterAccess() is not {} actor) return;
        
        transform.LookAt(actor.Transform.position, Vector3.up);
    }
}
