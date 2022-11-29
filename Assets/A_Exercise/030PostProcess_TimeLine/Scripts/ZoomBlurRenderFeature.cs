using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ZoomBlurRenderFeature : ScriptableRendererFeature
{

    class ZoomBlurPass : ScriptableRenderPass
    {
        static readonly string k_RenderTag = "Render ZoomBlur Effects";
        //与计算为ID，这样复制比字符串复制更快
        static readonly int MainTexID = Shader.PropertyToID("MainTex");
        static readonly int TempTargetID = Shader.PropertyToID("_TempTargetZoomBlur");
        static readonly int FocusPowerID = Shader.PropertyToID("_FocusPower");
        static readonly int FocusDetailID = Shader.PropertyToID("_FocusDetail");
        static readonly int FocusScreenPositionID = Shader.PropertyToID("_FocusScreenPosition");
        static readonly int ReferenceResolutionXID = Shader.PropertyToID("_ReferenceResolutionX");

        
        ZoomBlur zoomBlur;
        Material zoomBlurMaterial;
        RenderTargetIdentifier currentTarget;

        public ZoomBlurPass(RenderPassEvent evt)
        {
            //Debug.Log("ZoomBlur构造函数");
            this.renderPassEvent = evt;

            var shader = Shader.Find("LLY/PostEffect/ZoomBlur");
            if (shader)
                zoomBlurMaterial = CoreUtils.CreateEngineMaterial(shader);
            else
                Debug.LogError("未找到shader");
        }      

        public void Setup(in RenderTargetIdentifier currentTarget)
        {
            this.currentTarget = currentTarget;
        }

        void Render(CommandBuffer cmd,ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;

            var source = currentTarget;
            int des = TempTargetID;

            var w = cameraData.camera.scaledPixelWidth;
            var h= cameraData.camera.scaledPixelHeight;

            //把Volume组件ZoomBlur中值设置到材质中
            zoomBlurMaterial.SetFloat(FocusPowerID, zoomBlur.focusPower.value);
            zoomBlurMaterial.SetInt(FocusDetailID, zoomBlur.focusDetail.value);
            zoomBlurMaterial.SetVector(FocusScreenPositionID, zoomBlur.focusScreenPosition.value);
            zoomBlurMaterial.SetInt(ReferenceResolutionXID, zoomBlur.referenceResolutionX.value);

            int shaderIndex = 0;

            //cmd.SetGlobalTexture(MainTexID, source);

            cmd.GetTemporaryRT(des, w, h,0, FilterMode.Bilinear, RenderTextureFormat.Default);

            cmd.Blit(source, des);
            cmd.Blit(des, source, zoomBlurMaterial, shaderIndex);
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!zoomBlurMaterial || !renderingData.cameraData.postProcessEnabled)
                return;

            //继承的VolumeComponent的会被放入VolumeManager的栈中。
            zoomBlur = VolumeManager.instance.stack.GetComponent<ZoomBlur>();

            if (zoomBlur == null || !zoomBlur.IsActive())
                return;

            var cmd = CommandBufferPool.Get(k_RenderTag);

            Render(cmd,ref renderingData);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    ZoomBlurPass zoomBlurPass;
    public override void Create()
    {
        zoomBlurPass = new ZoomBlurPass(RenderPassEvent.AfterRenderingTransparents);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        zoomBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(zoomBlurPass);
    }

}
/// <summary>
/// 可以使用URP的Volume架构
/// </summary>
public class ZoomBlur : VolumeComponent, IPostProcessComponent
{
    public AnimationCurve test = new AnimationCurve(new Keyframe(0f, 0f, 1f, 1f), new Keyframe(1f, 1f, 1f, 1f));

    [Range(0, 100), Tooltip("加强效果是模糊效果更强")]
    public FloatParameter focusPower = new FloatParameter(0f);
    
    [Range(0, 10), Tooltip("值越大越好，但是负载增加")]
    public IntParameter focusDetail = new IntParameter(5);

    [Tooltip("模糊中心的坐标已经在屏幕的中心（0,0）")]
    public Vector2Parameter focusScreenPosition = new Vector2Parameter(Vector2.zero);

    [Tooltip("参考宽度分辨率")]
    public IntParameter referenceResolutionX = new IntParameter(1334);

    public bool IsActive() => true;

    public bool IsTileCompatible() => true;

}
