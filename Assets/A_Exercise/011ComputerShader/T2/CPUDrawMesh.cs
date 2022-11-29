using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPUDrawMesh : MonoBehaviour
{
    public Texture2D tex = null;
    public Material material = null;


    private Texture2D pallate = null;

    private Color[,] colors = null;

    int w =0;
    int h =0;

    private void Awake()
    {
        w = tex.width;
        h = tex.height;
        pallate = new Texture2D(w, h);
        colors = new Color[w, h];
    }
    private void Start()
    {
        MyGetPixel(tex);

        MySetPixel(colors);

        DisplayTex(pallate);
    }

    /// <summary>
    /// 得到图片的颜色信息并处理
    /// </summary>
    /// <param name="tex"></param>
    private void MyGetPixel(Texture2D tex)
    {
        Debug.Log("1");

        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                Color color = tex.GetPixel(i, j);
                if (color.grayscale<0.5)
                {
                    color.a = 0; ;
                }
                colors[i, j] = color;
            }
        }
    }

    //把得到的颜色画到新的Tex上
    private void MySetPixel(Color[,] colors)
    {
        for (int i = 0; i < w; i++)
        {
            for (int j = 0; j < h; j++)
            {
                pallate.SetPixel(i, j, colors[i, j]);
            }
        }
        pallate.Apply();
    }

    private void DisplayTex(Texture2D tex2D)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.transform.localScale = new Vector3(tex2D.width, tex2D.height, 0);
        go.GetComponent<Renderer>().sharedMaterial = material;
        material.mainTexture = tex2D;
    }
}
