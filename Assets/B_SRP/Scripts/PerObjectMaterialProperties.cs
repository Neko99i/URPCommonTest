using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 使用这种方式可以防止材质产生过多的Instance
/// </summary>
public class PerObjectMaterialProperties : MonoBehaviour
{
     int baseColorId = Shader.PropertyToID("_MainColor");

     MaterialPropertyBlock propertyBlock;

     public  Color baseColor = Color.white;

    void Awake()
    {
        OnValidate();
    }

    void OnValidate()
    {
        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock();

        Renderer renderer = GetComponentInChildren<Renderer>();

        propertyBlock.SetColor(baseColorId, GetRandomColor());

        renderer.SetPropertyBlock(propertyBlock);

        //下面这种会产生材质实例
        //renderer.material.SetColor(baseColorId, GetRandomColor());
    }

    Color GetRandomColor()
    {
        //return Color.HSVToRGB(Random.value, 1, .9f);
        return new Color(Random.value, 1, .9f, Random.value);
    }
}