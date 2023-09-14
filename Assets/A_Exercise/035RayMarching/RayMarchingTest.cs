using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class RayMarchingTest : ScriptableRendererFeature
{
    //定义一个设置的类
    [System.Serializable]
    public class MySetting
    {
        //public TextureCurve master = new TextureCurve(new[] { new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f) }, 0f, false, new Vector2(0f, 1f));
        //定义一个pass事件，并在透明物体之后渲染
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material mat;
        public int matPassIndex = -1;

        [Header("MaterialProperty"), Range(0, 3)]
        public float Brightness = 1;

        [Range(0, 3)]
        public float Saturation = 1;

        [Range(0, 3)]
        public float Contrast = 1f;
    }


    /// <summary>
    /// 自定义pass的渲染方式
    /// </summary>
    class CustomRenderPass:ScriptableRenderPass
    {
        public Material passMat = null;
        public int passMatInt = 0;
        public FilterMode passFilterMode { get; set;}  //图像模式
        private RenderTargetIdentifier passSrc { get; set; }  //源图像
        private RenderTargetHandle passTempTex;//临时图像

        string passTag;

        public float Brightness = 1;

        public float Saturation = 1;

        public float Contrast = 1f;

        //构造函数
        public CustomRenderPass(RenderPassEvent passEvent,Material mat,int passInt,string tag)
        {
            this.renderPassEvent = passEvent;
            this.passMat = mat;
            this.passMatInt = passInt;
            this.passTag = tag;
        }

        /// <summary>
        /// 接收RenderFeature传过来的源图
        /// </summary>
        /// <param name="src"></param>
        public void Setup(RenderTargetIdentifier src)
        {
            this.passSrc = src;

        }

        //执行渲染，OnRenderImage
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            passMat.SetFloat("_Brightness", Brightness);
            passMat.SetFloat("_Saturation", Saturation);
            passMat.SetFloat("_Contrast", Contrast);


            CommandBuffer cmd = CommandBufferPool.Get(passTag);
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;
            cmd.GetTemporaryRT(passTempTex.id, opaqueDesc, passFilterMode);

            //对取出来的源图像做后处理，放到tempTex上
            Blit(cmd, passSrc, passTempTex.Identifier(), passMat, passMatInt);
            //再把处理过的图渲染到源图像中
            Blit(cmd, passTempTex.Identifier(), passSrc);
            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);
            cmd.ReleaseTemporaryRT(passTempTex.id);

            //Debug.Log("执行渲染");
        }
    }

    public MySetting mySetting = new MySetting();

    private CustomRenderPass mypass;
    /// <summary>
    /// 生成一个自定义的pass
    /// </summary>
    public override void Create()
    {
        //计算材质球里的总的pass数，如果，没有则为1
        int passInt = mySetting.mat == null ? 1 : mySetting.mat.passCount - 1;
        //把设置里的ID限制在-1到材质的最大pass数
        mySetting.matPassIndex = Mathf.Clamp(mySetting.matPassIndex, -1, passInt);
        //实例化一下并传参数,name就是tag
        mypass = new CustomRenderPass(mySetting.passEvent, mySetting.mat, mySetting.matPassIndex, name);
        //Debug.Log("生成pass");

        //把值传入pass
        mypass.Saturation = mySetting.Saturation;
        mypass.Brightness = mySetting.Brightness;
        mypass.Contrast = mySetting.Contrast;
    }

    /// <summary>
    ///   传值到pass中
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="renderingData"></param>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //从这里得到源图传到pass中
        var src = renderer.cameraColorTarget;

        mypass.Setup(src);

        renderer.EnqueuePass(mypass);
        //Debug.Log("传图到自定义pass");

    }
}
