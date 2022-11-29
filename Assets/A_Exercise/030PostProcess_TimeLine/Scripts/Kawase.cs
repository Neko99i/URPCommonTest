using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Kawase : ScriptableRendererFeature
{
    //定义一个设置的类，从外面通过拖拽的方式传递数据
    [System.Serializable]
    public class MySetting
    {        
        
        //定义一个pass事件，并在透明物体之后渲染
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;

        public string passTag = "MyPassTag";
        public Material mat;

        [Header("MaterialProperty"),Range(0,2)]
        public int downSample = 1;

        [Range(1,10)]
        public int loop = 4;
                      
        [Range(0,1)]
        public float blurScale = 0.2f;

    }
    public MySetting mySetting = new MySetting();
    private CustomPass myPass;

    //Pass和数据的初始化
    public override void Create()
    {
        myPass = new CustomPass(mySetting.passTag);
        myPass.renderPassEvent = mySetting.passEvent;

        myPass.blurScale = mySetting.blurScale;
        myPass.loop = mySetting.loop;
        myPass.passMat = mySetting.mat;
        myPass.downSample = mySetting.downSample;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        myPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(myPass);
    }

    class CustomPass : ScriptableRenderPass
    {
        public Material passMat = null;

        public int downSample = 2;

        public int loop = 2;

        public float blurScale = 4;

        public FilterMode passFilterMode { get; set; }
        private RenderTargetIdentifier passSrc { get; set; }

        string passTag;

        public CustomPass(string tag)//构造函数
        {
            passTag = tag;
        }
        public void Setup(RenderTargetIdentifier sour)//接收render feather传的图
        {
            this.passSrc = sour;
        }

        //执行后处理
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {


            CommandBuffer cmd = CommandBufferPool.Get(passTag);
            cmd.name = "KawaseBlur";

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;

            int width = opaqueDesc.width >> downSample;
            int height = opaqueDesc.height >> downSample;

            opaqueDesc.depthBufferBits = 0;


            //申请两个临时RT用来交替进行blur渲染
            int bufferID1 = Shader.PropertyToID("rt1");
            int bufferID2 = Shader.PropertyToID("rt2");

            cmd.GetTemporaryRT(bufferID1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(bufferID2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            //buffer1 = new RenderTargetIdentifier(bufferID1);
            //buffer2 = new RenderTargetIdentifier(bufferID2);

            //cmd.SetGlobalFloat("_Blur", 1);
            passMat.SetFloat("_Blur", 1);
            cmd.Blit(passSrc, bufferID1, passMat);

            for (int i = 1; i < loop; i++)
            {
                //cmd.SetGlobalFloat("_Blur", i * blurScale + 1);
                passMat.SetFloat("_Blur", i * blurScale + 1);
                cmd.Blit(bufferID1, bufferID2, passMat);

                var tempRT = bufferID1;
                bufferID1 = bufferID2;
                bufferID2 = tempRT;
            }
            //cmd.SetGlobalFloat("_Blur", loop * blurScale + 1);
            passMat.SetFloat("_Blur", loop * blurScale + 1);

            cmd.Blit(bufferID1, passSrc, passMat);

            context.ExecuteCommandBuffer(cmd);//立即执行渲染命令

            cmd.ReleaseTemporaryRT(bufferID1);
            cmd.ReleaseTemporaryRT(bufferID2);

            CommandBufferPool.Release(cmd);

            Debug.Log("执行Kawase渲染");

        }
    }
}
