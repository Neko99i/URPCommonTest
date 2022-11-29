using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BSC_ComputerShader :  ScriptableRendererFeature
{
    //定义一个设置的类
    [System.Serializable]
    public class MySetting
    {
        //定义一个pass事件，并在透明物体之后渲染
        
        public ComputeShader CS = null;
        [Header("MaterialProperty"), Range(0, 3)]
        public float Brightness = 1;

        [Range(0, 3)]
        public float Saturation = 1;

        [Range(0, 3)]
        public float Contrast = 1f;

        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;

    }
    public MySetting mySetting;

    class BSC_CM : ScriptableRenderPass
    {
        private ComputeShader CS;

        private MySetting mySetting;

        private RenderTargetIdentifier Sour;

        public BSC_CM(MySetting mySetting)
        {
            this.mySetting = mySetting;

            CS = mySetting.CS;
        }
        
        public void Setup(RenderTargetIdentifier source)
        {
            this.Sour = source;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("ColorAdjust");

            int tempID = Shader.PropertyToID("temp1");

            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;

            desc.enableRandomWrite = true;

            cmd.GetTemporaryRT(tempID, desc);

            cmd.SetComputeFloatParam(CS, "_Bright", mySetting.Brightness);

            cmd.SetComputeFloatParam(CS, "_Saturate", mySetting.Saturation);

            cmd.SetComputeFloatParam(CS, "_Constrast", mySetting.Contrast);     

            cmd.SetComputeTextureParam(CS, 0, "_Result", tempID);

            cmd.SetComputeTextureParam(CS, 0, "_Sour", Sour);

            cmd.DispatchCompute(CS, 0, (int)desc.width / 8, (int)desc.height / 8, 1);

            cmd.Blit(tempID, Sour);

            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
        }
    }

    BSC_CM bsc;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (mySetting.CS)
        {
            bsc.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(bsc);
        }
        else
            Debug.LogWarning("CS为空");
    }
    public override void Create()
    {
        bsc = new BSC_CM(mySetting);

        bsc.renderPassEvent = mySetting.passEvent;
    }

}
