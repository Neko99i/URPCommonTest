using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RadiaBlur : ScriptableRendererFeature
{
    //定义一个设置的类，从外面通过拖拽的方式传递数据
    [System.Serializable]
    public class MySetting
    {

        //定义一个pass事件，并在透明物体之后渲染
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;

        public Material mat;

        [Header("MaterialProperty"), Range(0, 2)]
        public int downSample = 1;

        [Range(-1,1)]
        public float blurScale = 0;

        [Range(4, 100)]
        public int iteration = 10;

        public  Vector4 centerOffset = new Vector4(0.5f, 0.5f, 0, 0);

    }
    public MySetting mySetting = new MySetting();
    private CustomPass myPass;

    //Pass和数据的初始化
    public override void Create()
    {
        myPass = new CustomPass();
        myPass.renderPassEvent = mySetting.passEvent;

        myPass.blurScale = mySetting.blurScale;
        myPass.iteration = mySetting.iteration;
        myPass.passMat = mySetting.mat;
        myPass.centerOffset = mySetting.centerOffset;
        myPass.downSample = mySetting.downSample;
    }

    //把源图传入Pass
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        myPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(myPass);
    }

    class CustomPass : ScriptableRenderPass
    {
        public Material passMat = null;

        public int downSample = 2;

        public int iteration = 2;

        public float blurScale = 4;

        public Vector4 centerOffset = new Vector4(0.5f, 0.5f, 0, 0);

        private RenderTargetIdentifier passSrc { get; set; }

        public void Setup(RenderTargetIdentifier sour)//接收render feather传的图
        {
            this.passSrc = sour;
        }

        //执行后处理
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (passMat)
            {
                passMat.SetFloat("_BlurScale", blurScale);
                passMat.SetInt("_Iteration", iteration);
                passMat.SetVector("_CenterOffset", centerOffset);
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.name = "RadiaBlur";

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;

            int width = opaqueDesc.width >> downSample;
            int height = opaqueDesc.height >> downSample;

            opaqueDesc.depthBufferBits = 0;

            int bufferID = Shader.PropertyToID("rt");

         
           cmd.GetTemporaryRT(bufferID, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

    
            cmd.Blit(passSrc, bufferID, passMat);  //用后处理shader渲染


            cmd.Blit(bufferID, passSrc);    //把结果返回到源图中

            cmd.ReleaseTemporaryRT(bufferID);

            context.ExecuteCommandBuffer(cmd);//立即执行渲染命令


            CommandBufferPool.Release(cmd);


        }
    }
}
