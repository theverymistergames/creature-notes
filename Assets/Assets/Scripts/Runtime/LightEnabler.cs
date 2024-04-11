using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

public class LightEnabler : MonoBehaviour
{
    public GameObject dynamicObjects;
    
    // Start is called before the first frame update
    void Start()
    {
        var lights2 = dynamicObjects.GetComponentsInChildren<HDAdditionalLightData>();
        
        foreach (var light in lights2)
        {
            StartCoroutine(Test(light));
        }
    }

    IEnumerator Test(HDAdditionalLightData light)
    {
        yield return new WaitForSeconds(.5f);
        
        light.RequestShadowMapRendering();
    }
}
