using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 渲染灯光
/// </summary>
public class Lighting 
{
    const string bufferName = "Lighting";
    CommandBuffer cmd = new CommandBuffer() { name = bufferName };

    /// <summary>
    /// 灯光属性
    /// </summary>
    static int //dirLightColorId = Shader.PropertyToID("_DirectionalLightColor"),
    //           dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
                 dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
                 dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
                 dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

    static Vector4[] dirLightColors = new Vector4[maxDirLightCount],
                     dirLightDirections = new Vector4[maxDirLightCount];

    //在进行剔除时，Unity还会找出哪些光源会影响相机可见的空间。
    //我们可以依靠这些信息而不是全局的sun光源。 为此，Lighting需要访问剔除结果
    private  CullingResults cullingResults;

    /// <summary>
    /// 最大方向光数量
    /// </summary>
    const int maxDirLightCount = 4;

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
    {
        this.cullingResults = cullingResults;


        cmd.BeginSample(bufferName);

        SetupLights();

        cmd.EndSample(bufferName);

        ExecuteCmd(context);
        Debug.Log(dirLightColors[0]);
    }

    /// <summary>
    /// 多灯光可见数据
    /// </summary>
    void SetupLights()
    {
        //现在Lighting.SetupLights可以通过剔除结果的visibleLights属性检索所需的数据

        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        //这里最多只允许了4个灯光
        for (int i = 0; i <  visibleLights.Length; i++)
        {
            if (i>= maxDirLightCount)
                break;

            VisibleLight visibleLight = visibleLights[i];

            if (visibleLight.lightType == LightType.Directional)
                SetupDirectionalLight(i,ref visibleLight);
        }

        cmd.SetGlobalInt(dirLightCountId, visibleLights.Length<=maxDirLightCount? visibleLights.Length: maxDirLightCount);
        cmd.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        cmd.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
    }

    /// <summary>
    /// 设置灯光颜色和方向
    /// </summary>
    /// <param name="index"></param>
    /// <param name="visibleLight"></param>
    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        //  从renderSetting中的到场景的光照信息
        //https://docs.unity3d.com/ScriptReference/Light.html
        //Light light = RenderSettings.sun;

        //cmd.SetGlobalVector(dirLightColorId, light.color.linear*light.intensity);
        //cmd.SetGlobalVector(dirLightDirectionId, -light.transform.forward);

        //将索引和VisibleLight参数传递给SetupDirectionalLight。
        //用提供的索引设置颜色和光照方向。在这种情况下，最终颜色是通过VisibleLight.finalColor属性提供的。
        //可以通过VisibleLight.localToWorldMatrix属性找到方向矢量。
       
        //最终颜色已经应用了光源的强度，但是默认情况下Unity不会将其转换为线性空间。
        dirLightColors[index] = visibleLight.finalColor;
        //它是矩阵的第三列，必须再次取反。
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
    }

    void ExecuteCmd(ScriptableRenderContext context)
    {
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }


}
