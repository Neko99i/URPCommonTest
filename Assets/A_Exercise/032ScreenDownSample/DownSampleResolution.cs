using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DownSampleResolution : MonoBehaviour
{
    public float widthScale=1;
    public float heightScale=1;

    private float MaxW;
    private float MaxH;

    private bool isEx = false;
    void Start()
    {
      Resolution rs =  Screen.currentResolution;
      MaxW = rs.width;
      MaxH = rs.height;
      isEx = true;
      Debug.Log("start" + Screen.currentResolution.width);

    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(Screen.currentResolution.width);
    }

    private void LateUpdate()
    {
        if (isEx)
        {
            MaxW = MaxW * widthScale;
            MaxH = MaxH * heightScale;
            Screen.SetResolution((int)MaxW,(int)MaxH,  true);
            isEx = false;
            Debug.Log("EX" + MaxW);
        }

    }
}
