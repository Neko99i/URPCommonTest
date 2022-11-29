using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 给让Unity得到一个pipeline对象实例，负责渲染
/// </summary>
[CreateAssetMenu(menuName = "Rendering/Custom Render Pipleline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline( useDynamicBatching,  useGPUInstancing, useSRPBatcher);
    }
}