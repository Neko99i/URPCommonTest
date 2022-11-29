using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BrokeBlur : ScriptableRendererFeature
{
    //定义一个设置的类，从外面通过拖拽的方式传递数据
    [System.Serializable]
    public class MySetting
    {
        public Material mat;

        [Tooltip("降采样，越大性能越好但是质量越低"), Range(0,3)]
        public int downSample = 0;

        [Tooltip("迭代次数，越小性能越好但是质量越低"), Range(3, 500)]
        public int iteration = 50;

        [Tooltip("采样半径，越大圆斑越大但是采样点越分散"), Range(0.1f, 10)]
        public float R = 1;

        [Tooltip("模糊过渡的平滑度"), Range(0, 0.5f)]
        public float BlurSmoothness = 0.1f;

        [Tooltip("近处模糊结束距离")]
        public float NearDis = 5;

        [Tooltip("远处模糊开始距离")]
        public float FarDis = 9;

        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingOpaques;

    }
    public MySetting mySetting = new MySetting();
    private CustomPass myPass;

    //Pass和数据的初始化
    public override void Create()
    {
        myPass = new CustomPass();
        myPass.renderPassEvent = mySetting.passEvent;
        myPass.passMat = mySetting.mat;

        myPass.R = mySetting.R;
        myPass.iteration = mySetting.iteration;
        myPass.BlurSmoothness = mySetting.BlurSmoothness;
        myPass.downSample = mySetting.downSample;
        myPass.NearDis = mySetting.NearDis;
        myPass.FarDis = mySetting.FarDis;
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

        public int iteration = 50;

        public float R = 1;

        public float BlurSmoothness = 0.1f;

        public float NearDis = 5;

        public float FarDis = 9;

        private RenderTargetIdentifier passSrc { get; set; }

        //用于复制的临时RT需要是静态的
        readonly static int SourBakedID = Shader.PropertyToID("_SourTex");

        public void Setup(RenderTargetIdentifier sour)//接收render feather传的图
        {
            this.passSrc = sour;
        }

        //执行后处理
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (passMat)
            {
                passMat.SetFloat("_NearDis", NearDis);
                passMat.SetFloat("_FarDis", FarDis);
                passMat.SetInt("_Iteration", iteration);
                passMat.SetFloat("_Radius", R);
                passMat.SetFloat("_BlurSmoothness", BlurSmoothness);
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.name = "BrokeBlur";

            RenderTextureDescriptor dest = renderingData.cameraData.cameraTargetDescriptor;

            int width = dest.width >> downSample;
            int height = dest.height >> downSample;

            dest.depthBufferBits = 0;

            int blurID = Shader.PropertyToID("BlurRT");
         

            cmd.GetTemporaryRT(blurID, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(SourBakedID, dest);
            
            //把相机图像复制到备份RT图，并自动发送到shader里，无需手动指定发送
            cmd.CopyTexture(passSrc, SourBakedID);//这个RT不能是局部的

            //第一个pass:把屏幕图像计算后存到一个降采样的模糊图里
            cmd.Blit(passSrc, blurID, passMat, 0);

            //第二个pass:发送模糊图到shader的maintex,然后混合输出
            cmd.Blit(blurID, passSrc, passMat, 1);

            cmd.ReleaseTemporaryRT(blurID);
            cmd.ReleaseTemporaryRT(SourBakedID);

            context.ExecuteCommandBuffer(cmd);//立即执行渲染命令

            CommandBufferPool.Release(cmd);
        }
    }
}
