using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/Wireframe")]
public sealed class Wireframe : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

    Material m_Material;

    public bool IsActive() => m_Material != null;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;

    public override void Setup()
    {
        if (Shader.Find("Hidden/Shader/Wireframe") != null)
            m_Material = new Material(Shader.Find("Hidden/Shader/Wireframe"));
    }

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

        // m_Material.SetFloat("_Intensity", intensity.value);
        cmd.Blit(source, destination, m_Material, 0);
    }

    public override void Cleanup() => CoreUtils.Destroy(m_Material);

}