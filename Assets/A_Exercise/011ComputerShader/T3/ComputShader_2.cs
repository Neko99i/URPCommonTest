using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ComputShader_2 : MonoBehaviour
{
    public ComputeShader computeShader = null;

    struct data
    {
        public float r;
        public float g;
        public float b;
    };

    private void OnEnable()
    {
        InitData();
        ToComputeShader(inputDatas, outputDatas);
    }

    private data[] inputDatas;
    private data[] outputDatas;
    private void InitData()
    {
        inputDatas = new data[3];
        outputDatas = new data[3];

        for (int i = 0; i < inputDatas.Length; i++)
        {
            inputDatas[i].r = i + 1;
            inputDatas[i].g = i + 1;
            inputDatas[i].b = i + 1;
        }
    }

    private void ToComputeShader(data[] i, data[] o)
    {
        //data里数据是 3 个 4字节的float
        ComputeBuffer inputBuffer = new ComputeBuffer(i.Length, 12);
        ComputeBuffer outputBuffer = new ComputeBuffer(o.Length, 12);

        //拿到核心
        int k = computeShader.FindKernel("CSMain");

        //给申明的buffer写入数据
        inputBuffer.SetData(i);
        //传到GPU的computshader中声明的变量中
        computeShader.SetBuffer(k, "inputDatas", inputBuffer);
        computeShader.SetBuffer(k, "outputDatas", outputBuffer);

        //计算在输出到CPU
        computeShader.Dispatch(k, o.Length, 1, 1);
        outputBuffer.GetData(o);

        for (int j = 0; j < o.Length; j++)
        {
            Debug.Log(j + ":" + o[j].r);
            Debug.Log(j + ":" + o[j].g);
            Debug.Log(j + ":" + o[j].b);
        }

        inputBuffer.Release();
        outputBuffer.Release();
    }
}
