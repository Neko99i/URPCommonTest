using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScanningLine : ScriptableRendererFeature
{
    //定义一个设置的类，从外面通过拖拽的方式传递数据
    [System.Serializable]
    public class MySetting
    {
        public Material mat;


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

        private RenderTargetIdentifier passSrc { get; set; }

        public void Setup(RenderTargetIdentifier sour)//接收render feather传的图
        {
            this.passSrc = sour;
        }

        private Camera mc;
        public Camera MC
        {
            get { return Camera.main; }
        }

        //执行后处理
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (passMat)
            {
                Matrix4x4 frustumCorners = Matrix4x4.identity;

                //初步计算需要的参数
                float fov = MC.fieldOfView;
                float near = MC.nearClipPlane;
                float far = MC.farClipPlane;
                float aspect = MC.aspect;

                float halfH = near * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
                Vector3 toTop = MC.transform.up * halfH;
                Vector3 toRight = MC.transform.right * halfH * aspect;


                //相机到近裁剪面四个点向量
                Vector3 topLeft = MC.transform.forward * near - toRight + toTop;
                float scale = topLeft.magnitude / near;
                topLeft.Normalize();

                //后续只需要乘上深度即可
                topLeft *= scale;

                Vector3 topRight = MC.transform.forward * near + toRight + toTop;
                topRight.Normalize();
                topRight *= scale;

                Vector3 downRight = MC.transform.forward * near + toRight - toTop;
                downRight.Normalize();
                downRight *= scale;

                Vector3 downLeft = MC.transform.forward * near - toRight - toTop;
                downLeft.Normalize();
                downLeft *= scale;

                frustumCorners.SetRow(0, downLeft);
                frustumCorners.SetRow(1, downRight);
                frustumCorners.SetRow(2, topLeft);
                frustumCorners.SetRow(3, topRight);

                passMat.SetMatrix("_FrustumCornersRay", frustumCorners);
                //从右往左乘
                //material.SetMatrix("_ViewProjInverseM", (MyCamera.projectionMatrix * MyCamera.worldToCameraMatrix).inverse);
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            cmd.name = "ScanningLine";

            RenderTextureDescriptor dest = renderingData.cameraData.cameraTargetDescriptor;
            dest.depthBufferBits = 0;

            int width = dest.width ;
            int height = dest.height;

            int buffer1 = Shader.PropertyToID("rt1");

            cmd.GetTemporaryRT(buffer1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            cmd.Blit(passSrc, buffer1, passMat);

            cmd.Blit(buffer1, passSrc);

            cmd.ReleaseTemporaryRT(buffer1);

            context.ExecuteCommandBuffer(cmd);//立即执行渲染命令

            CommandBufferPool.Release(cmd);
        }
    }
}
