using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[Obsolete("No need to use this script. Check if shadows on demand are updated after scene load automatically")]
public sealed class LightsShadowMapRendererOnStart : MonoBehaviour
{
    [SerializeField] private GameObject dynamicObjects;
    
    private void Start() {
        var lights = dynamicObjects.GetComponentsInChildren<HDAdditionalLightData>();
        RequestShadowMapUpdateNextFrame(lights, destroyCancellationToken).Forget();
    }

    private static async UniTask RequestShadowMapUpdateNextFrame(HDAdditionalLightData[] lights, CancellationToken cancellationToken) {
        await UniTask.Yield();
        
        if (cancellationToken.IsCancellationRequested) return;
        
        for (int i = 0; i < lights.Length; i++) {
            lights[i].RequestShadowMapRendering();    
        }
    }
    
}
