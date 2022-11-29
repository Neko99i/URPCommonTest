using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor;
public class TextureBlenderCS : MonoBehaviour
{
    struct Resolution
    {
        public float width;
        public float height;

        public Resolution(float w,float h)
        {
            width = w;
            height = h;
        }
    }

    public float _VoroScale;
    public float _NoiseScale;
    public float _RotateScale;
    public ComputeShader cs;
    public Vector4 tilingOffset = new Vector4(1, 1, 0, 0);
    public Texture2D _Tex01;
    public Texture2D _Tex02;
    public Texture2D _Tex03;
    public Texture2D _Tex04;

    public Texture2D _Splatmap;

    private RenderTexture rt01;
    private int texID = Shader.PropertyToID("MainTex");

    private string path = "";

    private RenderTexture _tex01;
    private RenderTexture _tex02;
    private RenderTexture _tex03;
    private RenderTexture _tex04;

    private RenderTexture _splatmap;


    private void InitRT()
    {
        if (!_tex01)
        {
            _tex01 = new RenderTexture(512, 512, 0);
            _tex01.enableRandomWrite = true;
        }
        if (!_tex02)
        {
            _tex02 = new RenderTexture(512, 512, 0);
            _tex02.enableRandomWrite = true;
        }
        if (!_tex03)
        {
            _tex03 = new RenderTexture(512, 512, 0);
            _tex03.enableRandomWrite = true;
        }
        if (!_tex04)
        {
            _tex04 = new RenderTexture(512, 512, 0);
            _tex04.enableRandomWrite = true;
        }
        if (!_splatmap)
        {
            _splatmap = new RenderTexture(64, 64, 0);
            _splatmap.enableRandomWrite = true;
        }
    }

    void Start()
    {
        InitRT();

        path = SceneManager.GetActiveScene().path + "/../Texture";

        if (!rt01)
        {
            rt01 = new RenderTexture(512, 512, 24);
            rt01.enableRandomWrite = true;
            rt01.Create();

            this.GetComponent<MeshRenderer>().sharedMaterial.SetTexture(texID, rt01);
        }
    }


    private void Update()
    {
        if (cs && rt01)
        {
            RunTextureBlend();
        }
    }

    private void OnDisable()
    {
        rt01.Release();
        _tex01.Release();
        _tex02.Release();
        _tex03.Release();
        _splatmap.Release();
    }


    public void SavePNG()
    {
        
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        if (rt01)
        {
            byte[] bytes = RenderTextureToTexture2D(rt01).EncodeToPNG();

            File.WriteAllBytes(path + "/" + "blendTexture.png", bytes);
        }
    }

    private Texture2D RenderTextureToTexture2D(RenderTexture texture)
    {
        RenderTexture RT = RenderTexture.active;
        Texture2D texture2D = new Texture2D(texture.width, texture.height, TextureFormat.RGB24, false);
        RenderTexture.active = texture;
        texture2D.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = RT;

        return texture2D;
    }

    private RenderTexture Texture2DTORT(Texture2D texture)
    {
        RenderTexture RT = RenderTexture.active;

        //RenderTexture rt  = new RenderTexture(texture.width, texture.height, 0);
        RenderTexture rt  = RenderTexture.GetTemporary(texture.width, texture.height, 0);
        rt.enableRandomWrite = true;
        RenderTexture.active = rt;
        Graphics.Blit(texture, rt);
        RenderTexture.active = RT;
        return rt;
    }

    private RenderTexture Texture2DTORT( Texture2D texture , RenderTexture rt)
    {
        RenderTexture RT = RenderTexture.active;
        RenderTexture.active = rt;
        Graphics.Blit(texture, rt);
        RenderTexture.active = RT;
        return rt;
    }

    void RunTextureBlend()
    {
        int kernelHandle = cs.FindKernel("TextureBlend");

        Resolution res = new Resolution(rt01.width, rt01.height);
        Resolution[] resArr = new Resolution[] { res };
        ComputeBuffer resData = new ComputeBuffer(1, 8);
        resData.SetData(resArr);
        cs.SetBuffer(kernelHandle, "dataBuffer", resData);
        cs.SetTexture(kernelHandle, "Result", rt01);
        cs.SetVector("tilingOffset", tilingOffset);
        cs.SetFloat("_VoroScale", _VoroScale);
        cs.SetFloat("_NoiseScale", _NoiseScale);
        cs.SetFloat("_RotateScale", _RotateScale);

        //if (_Tex01 && _Tex02 && _Tex03 && _Splatmap)
        //{
        cs.SetTexture(kernelHandle, "_Tex01", _Tex01);
        cs.SetTexture(kernelHandle, "_Tex02", _Tex02);
        cs.SetTexture(kernelHandle, "_Tex03", _Tex03);
        cs.SetTexture(kernelHandle, "_Tex04", _Tex04);
        cs.SetTexture(kernelHandle, "_Splatmap", _Splatmap);

        cs.Dispatch(kernelHandle, rt01.width / 8, rt01.height / 8, 1);

        //}
        //else Debug.LogError("执行错误");

        resData.Release();

    }
}
