using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualBlur : ScriptableRendererFeature
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

        [Range(1, 10)]
        public int loop = 2;

        [Range(0,10)]
        public float blurScale = 1f;

    }
    public MySetting mySetting = new MySetting();
    private CustomPass myPass;

    //Pass和数据的初始化
    public override void Create()
    {
        myPass = new CustomPass();
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

        private RenderTargetIdentifier passSrc { get; set; }


        struct Level
        {
            public int down;
            public int up;
        };

        Level[] my_level;
        int maxLevel = 16;

        public void Setup(RenderTargetIdentifier sour)//接收render feather传的图
        {
            this.passSrc = sour;
            my_level = new Level[maxLevel];

            for (int i = 0; i < maxLevel; i++)
            {
                my_level[i].down = Shader.PropertyToID("_BlurMipDown" + i);
                my_level[i].up = Shader.PropertyToID("_BlurMipUp" + i);
            }
        }

        //执行后处理
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.name = "DualBlur";
            passMat.SetFloat("_Blur", blurScale);

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;

            int width = opaqueDesc.width >> downSample;
            int height = opaqueDesc.height >> downSample;

            opaqueDesc.depthBufferBits = 0;

            //降采样Blur部分
            RenderTargetIdentifier tempRT = passSrc;

            for (int i = 0; i < loop; i++)
            {
                int midDown = my_level[i].down;

                width = Mathf.Max(width / 2, 1);
                height = Mathf.Max(height / 2, 1);

                cmd.GetTemporaryRT(midDown, width, height,0 ,FilterMode.Bilinear);


                cmd.Blit(tempRT, midDown,passMat,0);

                tempRT = midDown;

                //cmd.ReleaseTemporaryRT(midDown); //释放临时申请的RT
            }

            //升采样Blur部分
            for (int i = 0; i < loop-1; i++)
            {
                int midUp = my_level[i].up;

                width = width * 2;
                height = height * 2;

                cmd.GetTemporaryRT(midUp, width, height, 0, FilterMode.Bilinear);


                cmd.Blit(tempRT, midUp, passMat, 1);

                tempRT = midUp;

                //cmd.ReleaseTemporaryRT(midUp); //释放临时申请的RT
            }

            cmd.Blit(tempRT, passSrc, passMat, 1);

            for (int i = 0; i < loop; i++)
            {
                cmd.ReleaseTemporaryRT(my_level[i].down);
            }
            for (int i = 0; i < loop-1; i++)
            {
                cmd.ReleaseTemporaryRT(my_level[i].up);
            }

            context.ExecuteCommandBuffer(cmd);//执行命令缓冲区的该命令

            CommandBufferPool.Release(cmd);//释放cmd
        }
    }
}
