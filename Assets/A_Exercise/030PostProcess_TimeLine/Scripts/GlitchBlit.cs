using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

public class GlitchBlit : ScriptableRendererFeature
{
    [System.Serializable]
    public class MySetting
    {
        public Material mat = null;

        [Range(0, 1)] public float Instensity = 0.5f;

        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public MySetting setting;
    class GlitchColorSplit : ScriptableRenderPass
    {
        MySetting mysetting = null;

        RenderTargetIdentifier sour;

        public void stetup(RenderTargetIdentifier source)
        {
            this.sour = source;
        }

        public GlitchColorSplit(MySetting setting)
        {
            mysetting = setting;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            mysetting.mat.SetFloat("_Instensity", mysetting.Instensity);

            CommandBuffer cmd = CommandBufferPool.Get("GlitchColorSplit");

            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;

            int sourID = Shader.PropertyToID("_SourTex");

            cmd.GetTemporaryRT(sourID, desc);

            cmd.CopyTexture(sour, sourID);

            cmd.Blit(sourID, sour, mysetting.mat);

            context.ExecuteCommandBuffer(cmd);

            cmd.ReleaseTemporaryRT(sourID);

            CommandBufferPool.Release(cmd);
        }
    }

    GlitchColorSplit m_ColorSplit;

    public override void Create()
    {
        m_ColorSplit = new GlitchColorSplit(setting);

        m_ColorSplit.renderPassEvent = setting.passEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_ColorSplit.stetup(renderer.cameraColorTarget);

        renderer.EnqueuePass(m_ColorSplit);
    }


}
