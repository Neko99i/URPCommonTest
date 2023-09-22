using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 自定义渲染管线
/// </summary>
public class CustomRenderPipeline : RenderPipeline
{
    //在构造函数中开启SRPBatcher
    //Unity不比较材料的精确内存布局，它只是简单地批量处理使用完全相同的变量的draw call。

    bool useDynamicBatching, useGPUInstancing;
    public CustomRenderPipeline(bool useDynamicBatching,bool useGPUInstancing , bool useSRPBatcher)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }

    //Unity每帧都会在RP实例上调用Render方法，它有一个context结构体参数，（命令队列）
    //其提供与原生引擎之间的连接，我们可以用来进行渲染，
    //还有一个摄像机队列参数，我们可以按顺序渲染多个摄像机。
    CameraRender renderer = new CameraRender();
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera camera in cameras)
        {
            renderer.Render(context, camera,  useDynamicBatching,  useGPUInstancing);
        }
    }
}

public class CameraRender
{
    /// <summary>
    /// 内置渲染命令Buffer
    /// </summary>
    ScriptableRenderContext context;

    /// <summary>
    /// 当前渲染的相机
    /// </summary>
    Camera camera;

    /// <summary>
    /// 当前相机的裁剪结果
    /// </summary>
    CullingResults cullingResults;

    /// <summary>
    /// 指定哪种shader可以被渲染 ,这个很重要 ，可以传入TagID数组
    /// </summary>
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId customShaderTagID = new ShaderTagId("CustomMode");

    const string bufferName = "Render Camera";
    string SampleName = bufferName;

    CommandBuffer cmd = new CommandBuffer { name = bufferName };

    /// <summary>
    /// 在CameraRender之前处理Light渲染
    /// </summary>
    Lighting lighting = new Lighting();

    public void Render(ScriptableRenderContext context, Camera camera, bool useDynamicBatching, bool useGPUInstancing)
    {
        //初始化
        this.context = context;
        this.camera = camera;

        //为了把相机的渲染分开，通过名字区分
        cmd.name = SampleName = camera.name;

        //绘制UI，并且不会被裁剪
        PrepareForSceneWindow();

        //如果裁剪就不渲染
        if (!Cull())
            return;

        Setup();

        lighting.Setup(context, cullingResults);


        DrawRender(useDynamicBatching,useGPUInstancing);

        #region Editor下运行
#if UNITY_EDITOR

        DrawGizmos();

#endif
        #endregion

        //三：执行命令
        Submit();
    }


    /// <summary>
    /// 画可视化物体
    /// </summary>
    void DrawRender(bool useDynamicBatching, bool useGPUInstancing)
    {
        //1.渲染不透明物体
        DrawOpaque( useDynamicBatching,useGPUInstancing);

        //2.绘制错误的物体
        #region Editor下运行
#if UNITY_EDITOR
        DrawUnsupportedShaders();
#endif
        #endregion

        //3. DrawSky 这里只是把命令放入到Context中
        context.DrawSkybox(camera);

        //4.渲染透明物体   
        DrawTransparent();
    }

    /// <summary>
    /// 画不透明物体
    /// </summary>
    void DrawOpaque(bool useDynamicBatching, bool useGPUInstancing)
    {
            //安全的绘制
            cmd.Clear();
            //渲染设置   //初始化渲染设置 
            //排序设置     从前向后绘制  （SortingCriteria）
            var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };

            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
            {
                enableDynamicBatching = useDynamicBatching,
                enableInstancing = useGPUInstancing
            };
            //把自定义的LightMode加入到ShaderTags中
            drawingSettings.SetShaderPassName(1, customShaderTagID);

            //这里先保证只渲染不透明物体
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

    }

    /// <summary>
    /// 画透明物体
    /// </summary>
    void DrawTransparent()
    {

            //重新设置渲染队列和排序 (透明的)
            var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonTransparent };
            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
            //这里先保证只渲染不透明物体
            var filteringSettings = new FilteringSettings(RenderQueueRange.transparent);

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

    }

    /// <summary>
    /// 是否裁剪
    /// </summary>
    /// <returns></returns>
    private bool Cull()
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters cp))
        {
            cullingResults = context.Cull(ref cp);
            return true;
        }
        return false;
    }


    /// <summary>
    /// 执行CommonBuffer并清理缓存命令
    /// </summary>
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }

    /// <summary>
    /// 初始配置
    /// </summary>
    void Setup()
    {
        //一：构建视角投影矩阵，并传递给CommonBuffer
        context.SetupCameraProperties(camera);

        ClearBuffer();

        //可以同时显示在profiler和frame debugger面板
        cmd.BeginSample(SampleName);

        ExecuteBuffer();

    }

    /// <summary>
    /// 执行命令函数
    /// </summary>
    void Submit()
    {
        cmd.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    /// <summary>
    /// 清理颜色深度+Stencil缓存
    /// </summary>
    void ClearBuffer()
    {
        cmd.ClearRenderTarget(true, true, Color.clear);
    }

    #region Editor下运行

#if UNITY_EDITOR

    //除了主Tag外的tags
    static ShaderTagId[] legacyShaderTagIds =
    {
            new ShaderTagId("Always"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM") ,
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("LightweightForward")
            //new ShaderTagId("SRPDefaultUnlit")
    };

    //当渲染错误时用来显示的紫色shader材质
    static Material errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));

    /// <summary>
    /// 可以渲染指定shaderTag之外的物体，但是使用紫色材质渲染
    /// </summary>
    void DrawUnsupportedShaders()
    {
            var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera));
            drawingSettings.overrideMaterial = errorMaterial;

            //把所有需要渲染的shaderTag都设置进去
            for (int i = 1; i < legacyShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
            }
            var filteringSettings = FilteringSettings.defaultValue;

            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

    }

    /// <summary>
    /// 在Scene画辅助线
    /// </summary>
    void DrawGizmos()
    {
        //Handles.ShouldRenderGizmos可以检查unity是否打开绘制Gizmo
        //Gizmo有两个子集，分别是前图像处理的子集和后图像处理的子集，
        //由于我们这没有实现图像处理，所以在这对两个子集都进行绘制
        //camera.cameraType==CameraType.SceneView保证在Scene下才绘制
        if (Handles.ShouldRenderGizmos() && camera.cameraType == CameraType.SceneView)
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    /// <summary>
    /// 将UI添加到世界空间中,并且在Scene中绘制
    /// </summary>
    void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }


#endif
    #endregion

}


