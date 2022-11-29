using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CS_Controller : MonoBehaviour
{
    public Texture inputTex;
    public ComputeShader computeShader;
    public RawImage image;

    public Material mat;
    private void Start()
    {
        RenderTexture rt = new RenderTexture(512, 512, 0);
        rt.enableRandomWrite = true;
        rt.Create();
        mat.SetTexture("_MainTex", rt);
        image.texture = rt;
        image.SetNativeSize();

        int kernel = computeShader.FindKernel("Gray");

        computeShader.SetTexture(kernel, "inputTex", inputTex);
        computeShader.SetTexture(kernel, "outputTex", rt);

        computeShader.Dispatch(kernel, rt.width / 8, rt.height / 8, 1);
    }
}
