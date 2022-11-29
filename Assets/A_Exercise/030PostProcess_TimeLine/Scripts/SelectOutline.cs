using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SelectOutline: ScriptableRendererFeature
{           
    public enum BloomType
    {
        InColorON,
        InColorOFF,
    }

    [System.Serializable]
    public class MySetting
    {
        public Material mymat;
        public Color color = Color.blue;

        [Range(1000, 5000)]
        public int queueMin = 2000;

        [Range(1000, 5000)]
        public int queueMax = 2500;

        /// <summary>
        /// 渲染layermask
        /// </summary>
        public LayerMask layer;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingSkybox;

        [Range(0, 3)]
        public float blur = 1;

        [Range(1, 5)]
        public int iteration = 3;

        public BloomType bloomType = BloomType.InColorON;
    }
    public MySetting mySetting = new MySetting();

    int soildaColorID;

    Calculate m_Calculate;
    DrawSolidColorPass m_DrawSoildColorPass;

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (mySetting.mymat != null)
        {
            //传入源图
            RenderTargetIdentifier sour = renderer.cameraColorTarget;
            //设置pass  index为0
            renderer.EnqueuePass(m_DrawSoildColorPass);

            m_Calculate = new Calculate(mySetting, this, sour);
            m_Calculate.renderPassEvent = mySetting.passEvent;
            renderer.EnqueuePass(m_Calculate);
        }
        else
        {
            Debug.LogError("材质球丢失！请设置材质球");
        }
       
    }
    //Pass和数据的初始化
    public override void Create()
    {
        m_DrawSoildColorPass = new DrawSolidColorPass(mySetting, this);

        m_DrawSoildColorPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
    }

    class DrawSolidColorPass : ScriptableRenderPass
    {
        MySetting mySetting = null;

        SelectOutline SelectOutline = null;

        //只有在这个标签LightMode对应的shader才会被绘制
        ShaderTagId shaderTag = new ShaderTagId("DepthOnly");

        //该pass 队列和filterMask
        FilteringSettings filter;


        //构造函数
        public DrawSolidColorPass(MySetting setting, SelectOutline render)
        {
            mySetting = setting;

            SelectOutline = render;

            //过滤设定
            RenderQueueRange queue = new RenderQueueRange();

            queue.lowerBound = Mathf.Min(setting.queueMax, setting.queueMin);

            queue.upperBound = Mathf.Max(setting.queueMax, setting.queueMin);

            //这里指定了该pass渲染的层级
            filter = new FilteringSettings(queue, setting.layer);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {

            int temp = Shader.PropertyToID("_MyTempColor1");

            RenderTextureDescriptor desc = cameraTextureDescriptor;

            cmd.GetTemporaryRT(temp, desc);

            SelectOutline.soildaColorID = temp;

            ConfigureTarget(temp);

            ConfigureClear(ClearFlag.All, Color.black);

        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //传入材质属性
            if (mySetting.mymat)
            {
                mySetting.mymat.SetColor("_SoildColor", mySetting.color);
            }
            CommandBuffer cmd = CommandBufferPool.Get("GetBaseColor");

            //安全的绘制
            using (new ProfilingScope(cmd,new ProfilingSampler("GetBaseColor")))
            {
                context.ExecuteCommandBuffer(cmd);

                cmd.Clear();

                //绘制设置
                var draw = CreateDrawingSettings(shaderTag, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);

                draw.overrideMaterial = mySetting.mymat;

                draw.overrideMaterialPassIndex = 0;

                //开始绘制（准备好了绘制设定和过滤设定）
                context.DrawRenderers(renderingData.cullResults, ref draw, ref filter);

                context.ExecuteCommandBuffer(cmd);

                CommandBufferPool.Release(cmd);
            }
            
        }
    }

    class Calculate : ScriptableRenderPass
    {
        MySetting mySetting = null;

        SelectOutline selectOutline = null;

        struct LEVEL
        {
            public int down;
            public int up;
        };

        LEVEL[] my_level;

        int maxLevel = 16;

        RenderTargetIdentifier sour;

        public Calculate (MySetting setting ,SelectOutline render,RenderTargetIdentifier source)
        {
            mySetting = setting;
            selectOutline = render;
            sour = source;
            my_level = new LEVEL[maxLevel];

            //申请32个ID的，up和down各16个，用这个id去代替临时RT来使用
            for (int t = 0; t < maxLevel; t++)
            {
                my_level[t] = new LEVEL
                {
                    down = Shader.PropertyToID("_BlurMipDown" + t),
                    up = Shader.PropertyToID("_BlurMipUp" + t)
                };
            }

            if (mySetting.bloomType == BloomType.InColorON)
            {
                mySetting.mymat.EnableKeyword("_INCOLOR_ON");
                mySetting.mymat.DisableKeyword("_INCOLOR_OFF");
            }
            else
            {
                mySetting.mymat.EnableKeyword("_INCOLOR_OFF");
                mySetting.mymat.DisableKeyword("_INCOLOR_ON");
            }
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("颜色计算");

            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;

            int SourID = Shader.PropertyToID("_SourTex");

            cmd.GetTemporaryRT(SourID, desc);

            cmd.CopyTexture(sour, SourID);


            //计算双重kawase模糊
            int BlurID = Shader.PropertyToID("_BlurTex");

            cmd.GetTemporaryRT(BlurID, desc);

            mySetting.mymat.SetFloat("_Blur", mySetting.blur);

            int width = desc.width / 2;

            int height = desc.height / 2;

            int LastDown = selectOutline.soildaColorID;

            for (int t = 0; t < mySetting.iteration; t++)
            {
                int midDown = my_level[t].down;//middle down ，即间接计算down的工具人ID

                int midUp = my_level[t].up; //middle Up ，即间接计算的up工具人ID

                cmd.GetTemporaryRT(midDown, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);//对指定高宽申请RT，每个循环的指定RT都会变小为原来一半

                cmd.GetTemporaryRT(midUp, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);//同上，但是这里申请了并未计算，先把位置霸占了，这样在UP的循环里就不用申请RT了

                cmd.Blit(LastDown, midDown, mySetting.mymat, 1);//计算down的pass

                LastDown = midDown;

                width = Mathf.Max(width / 2, 1);//每次循环都降尺寸

                height = Mathf.Max(height / 2, 1);
            }

            //up
            int lastUp = my_level[mySetting.iteration - 1].down;//把down的最后一次图像当成up的第一张图去计算up

            for (int j = mySetting.iteration - 2; j >= 0; j--)//这里减2是因为第一次已经有了要减去1，但是第一次是直接复制的，所以循环完后还得补一次up
            {
                int midUp = my_level[j].up;

                cmd.Blit(lastUp, midUp, mySetting.mymat, 2);

                lastUp = midUp;
            }

            cmd.Blit(lastUp, BlurID, mySetting.mymat, 2);//补一个up，顺便在模糊一下

            cmd.Blit(selectOutline.soildaColorID, sour, mySetting.mymat, 3);//在第4个pass里合并所有图像

            context.ExecuteCommandBuffer(cmd); 

            //回收
            for (int k = 0; k < mySetting.iteration; k++)
            {
                cmd.ReleaseTemporaryRT(my_level[k].up);
                cmd.ReleaseTemporaryRT(my_level[k].down);
            }

            cmd.ReleaseTemporaryRT(BlurID);

            cmd.ReleaseTemporaryRT(SourID);

            cmd.ReleaseTemporaryRT(selectOutline.soildaColorID);

            CommandBufferPool.Release(cmd);

        }
    }
    

}  
