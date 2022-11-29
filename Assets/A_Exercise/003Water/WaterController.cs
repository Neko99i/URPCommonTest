using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class WaterController : MonoBehaviour
{
    public Vector4 center = new Vector4(0.5f, 0.5f, 0, 0);

    [Header("——————————————————影响参数————————————————")]
    //距离系数
    public float frequencyFactor = 60.0f;
    //时间系数
    public float timeFactor = 10.0f;
    //sin函数结果系数
    public float totalFactor = 1.0f;

    //波纹宽度
    public float waveWidth = 0.3f;
    //波纹扩散的速度
    public float waveSpeed = 0.3f;

    public float waveDistance = 1.0f;

    [Header("程序控制")]
    [Range(0,1)]
    public int isBlink = 0;
    public Color blinkColor = new Color(1,1,1,1);

    private float waveStartTime;

    private Material waterMat;

    private float isWave = 0;

    private void Awake()
    {
        if (GetComponent<MeshRenderer>())
        {
            if (GetComponent<MeshRenderer>().sharedMaterial)
            {
                waterMat = GetComponent<MeshRenderer>().sharedMaterial;
            }
        }
    }

    void Update()
    {

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 1000, 1 << LayerMask.NameToLayer("Water")))
            {
                 Vector3 hitPoint= hit.point;
                waveStartTime = Time.time;
                center = new Vector4(-hitPoint.x/100, -hitPoint.z/100, 0);
            }

            isWave = 1;
        }


        float curWaveDistance = (Time.time - waveStartTime) * waveSpeed;
        isWave = curWaveDistance >= waveDistance ? 0 : isWave;
        //设置一系列参数
        waterMat.SetFloat("_distanceFactor", frequencyFactor);
        waterMat.SetFloat("_timeFactor", timeFactor);
        waterMat.SetFloat("_totalFactor", totalFactor * isWave);
        waterMat.SetFloat("_waveWidth", waveWidth);
        waterMat.SetFloat("_curWaveDis", curWaveDistance);
        waterMat.SetFloat("_isBlink", isBlink);
        waterMat.SetColor("_blinkColor", blinkColor);
        waterMat.SetVector("_center", center + new Vector4(0.5f, 0.5f, 0, 0));

    }



}
