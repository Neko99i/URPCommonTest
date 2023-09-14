using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using ShadowQuality = UnityEngine.ShadowQuality;

//摄像机跟随脚本

public class TestUI : MonoBehaviour
{
    private float renderScale = 1;
    private bool isFSR = false;
    private int texQuality = 0;

    private bool isFPS = true;

    // private int maxFrame = 60;
    private int lockFrame = 60;
    private bool m_showGUIButtons = false;
    private bool m_isBloom = true;
    private bool m_isFog = true;

    //后处理相关
    public Volume m_volume;
    private Bloom m_bloomVolume;
    private int m_Interations = 5;
    private int m_DownSamples = 1;

    private Transform player; //1.1声明player；
    private string qualityLevel;


    float updateInterval = 1.0f;
    private float accum = 0.0f;
    private float frames = 0;

    private float timeleft;
    private float fps = 15.0f;
    private float lastSample;
    private float wholeGotIntervals = 0;


    private bool isShowBloom = true;

    private GameObject wholeGo;

    public List<GameObject> gos;
    private int goIndex = 0;

    public GameObject roleUI;

    public GameObject sceneVFX;

    void Start()
    {
        // //保障AB资源加载可以用，不依赖Laucher单独启动测试scene的时候需要
        // ResourceMgrConfig resourceConfig = new ResourceMgrConfig();
        // resourceConfig.assetsPath = "";
        //
        // ResourceMgr.Instance.Init(resourceConfig);
  
    }

    int GetScaledWidth(int value)
    {
        float ratio = value / 2360f;
        var result = Screen.width * ratio;
        return (int) result;
    }

    int GetScaledHeight(int value)
    {
        float ratio = value / 1024f;
        var result = Screen.height * ratio;
        return (int) result;
    }

    private int shadowLevel = 1;
    private int msaaCount = 0;

    private void OnGUI()
    {
        var rect = new Rect();
        rect.x = GetScaledWidth(1800);
        rect.y = GetScaledHeight(50);
        rect.width = GetScaledWidth(800);
        rect.height = GetScaledHeight(50);

        GUIStyle fontStyle = new GUIStyle();
        fontStyle.fontSize = GetScaledWidth(40);
        fontStyle.normal.textColor = Color.white;
        if (isFPS == true)
        {
            var level = "当前等级：" + qualityLevel;
            GUI.Label(rect, level, fontStyle);

            // rect.y += GetScaledHeight(50);
            // GUI.Label(rect, "多线程渲染：" + SystemInfo.graphicsMultiThreaded, fontStyle);

            rect.y += GetScaledHeight(50);
            string tips = "帧率: " + fps.ToString();
            GUI.Label(rect, tips, fontStyle);
        }


        var leftBottomRect = rect;
        leftBottomRect.x = GetScaledWidth(50);
        leftBottomRect.y = GetScaledHeight(900);
        leftBottomRect.width = GetScaledWidth(200);


        if (GUI.Button(leftBottomRect, m_showGUIButtons ? "隐藏所有按钮" : "显示所有按钮"))
        {
            m_showGUIButtons = !m_showGUIButtons;
        }

        var leftBottomRect2 = rect;
        leftBottomRect2.x = GetScaledWidth(50);
        leftBottomRect2.y = GetScaledHeight(800);
        leftBottomRect2.width = GetScaledWidth(100);
        if (GUI.Button(leftBottomRect2, m_showGUIButtons ? "隐藏帧率" : "显示帧率"))
        {
            isFPS = !isFPS;
        }

        if (m_showGUIButtons)
        {
            // rect.y += GetScaledHeight(50);
            // // GUI.Label(rect, "阴影：" +    gmInstance.ShadowQualityLevel , fontStyle);
            // GUI.Label(rect, "BloomDowns：" +  m_bloomVolume.downSample.value, fontStyle);
            // rect.y += GetScaledHeight(50);
            //
            // GUI.Label(rect, "BloomInterations：" +  m_bloomVolume.maxIterations.value, fontStyle);

            rect.y += GetScaledHeight(50);
            rect.width = GetScaledWidth(400);
            if (GUI.Button(rect, $"改变目标帧率({lockFrame})"))
            {
                lockFrame += 10;
                lockFrame = lockFrame > 60 ? 30 : lockFrame;
                Application.targetFrameRate = lockFrame;
            }

            rect.x = GetScaledWidth(50);
            rect.y = GetScaledHeight(50);
            rect.width = GetScaledWidth(200);

            if (GUI.Button(rect, "<size=20>R++</size>"))
            {
                renderScale += 0.1f;
                renderScale = renderScale >= 2 ? 2 : renderScale;

                UniversalRenderPipeline.asset.renderScale = renderScale; //suchao   用UniversalRenderPipeline.asset.renderScale
            }

            rect.x += GetScaledWidth(210);
            if (GUI.Button(rect, "<size=20>R--</size>"))
            {
                renderScale -= 0.1f;
                renderScale = renderScale <= 0.0f ? 0.0f : renderScale;
                UniversalRenderPipeline.asset.renderScale = renderScale;
            }

            rect.x += GetScaledWidth(210);
            if (GUI.Button(rect, "<size=20>R==1</size>"))
            {
                renderScale = 1f;
                UniversalRenderPipeline.asset.renderScale = renderScale;
            }


            // rect.x = GetScaledWidth(50); 
            // rect.y = GetScaledHeight(140);
            // rect.width = GetScaledWidth(200);
            // if (GUI.Button(rect, "<size=20>Bloom</size>"))
            // {
            //     m_bloomVolume.active = !m_bloomVolume.active;
            // }
            //
            //
            // if (m_bloomVolume.active)
            // {
            //     rect.x += GetScaledWidth(210);
            //     if (  GUI.Button(rect, "<size=20>BloomIter</size>"))
            //     {
            //         m_Interations = ++ m_Interations  > 5 ? 1 : m_Interations;
            //         m_bloomVolume.maxIterations .value = m_Interations;
            //     }
            //     
            //     rect.x += GetScaledWidth(210);
            //     if (  GUI.Button(rect, "<size=20>BloomDown</size>"))
            //     {
            //         m_DownSamples = ++ m_DownSamples  > 4 ? 1 : m_DownSamples;
            //         m_bloomVolume.downSample .value = m_DownSamples;
            //     }
            //     
            // }
            //
            
            // if (GUI.Button(rect, "<size=20>Shadow</size>"))
            // {
            //     shadowLevel = ++shadowLevel > 3 ? 0 : shadowLevel;
            //     gmInstance.ShadowQualityLevel = (ShadowQualityLevel)shadowLevel;
            //     // gmInstance.UpdateGraphicSettings();
            //
            //     // UniversalRenderPipeline.asset.supportsMainLightShadowsCustom = !UniversalRenderPipeline.asset.supportsMainLightShadowsCustom;
            // }
            //
            // rect.x += GetScaledWidth(210);
            // if (GUI.Button(rect, "<size=20>Msaa</size>"))
            // {
            //     msaaCount = ++msaaCount > 2 ? 0 : msaaCount;
            //     gmInstance.MsaaSampleCount = (MsaaQuality)Mathf.Pow(2, msaaCount);
            //     // gmInstance.UpdateGraphicSettings();
            //     // UniversalRenderPipeline.asset.msaaSampleCount = (int)Mathf.Pow(2, msaaCount);
            //
            //     UniversalRenderPipeline.asset.msaaSampleCount = (int) Mathf.Pow(2, msaaCount);
            // }
            //
            // rect.x += GetScaledWidth(210);
            // if (GUI.Button(rect, "<size=20>ChangeRole</size>"))
            // {
            //     // goIndex = ++goIndex >= gos.Count ? 0:goIndex;
            //     // for (int i = 0; i < gos.Count; i++)
            //     // {
            //     //     if (i==goIndex)
            //     //     {
            //     //         gos[i].SetActive(true);
            //     //     }
            //     //     else
            //     //     {
            //     //         gos[i].SetActive(false);
            //     //     }
            //     // }
            //     this.gameObject.SetActive(!this.gameObject.activeSelf);
            // }
            //
            //
            //
            // rect.x = GetScaledWidth(50);
            // rect.y = GetScaledHeight(240);
            // rect.width = GetScaledWidth(200);
            //
            // if (GUI.Button(rect, "<size=20>VFX01</size>"))
            // {
            //     roleUI.SetActive(!roleUI.activeSelf);
            // }
            //
            // rect.x += GetScaledWidth(210);
            // if (GUI.Button(rect, "<size=20>VFX02d`</size>"))
            // {
            //     sceneVFX.SetActive(!sceneVFX.activeSelf);
            // }
            
            // rect.x = GetScaledWidth(50);
            // rect.y = GetScaledHeight(240);
            // rect.width = GetScaledWidth(200);
            //
            // if (GUI.Button(rect, "<size=20>原画</size>"))
            // {
            //     bool isSuccess = gmInstance.SetResolutionLevel(GraphicManager.ResolutionLevel.OPD);
            //     LogSetResult(isSuccess);
            // }
            //
            // rect.x += GetScaledWidth(210);
            //
            // if (GUI.Button(rect, "<size=20>高(R)</size>"))
            // {
            //     bool isSuccess = gmInstance.SetResolutionLevel(GraphicManager.ResolutionLevel.High);
            //     LogSetResult(isSuccess);
            // }
            //
            // rect.x += GetScaledWidth(210);
            // if (GUI.Button(rect, "<size=20>中(R)</size>"))
            // {
            //     bool isSuccess = gmInstance.SetResolutionLevel(GraphicManager.ResolutionLevel.Mid);
            //     LogSetResult(isSuccess);
            // }
            //
            // rect.x += GetScaledWidth(210);
            // if (GUI.Button(rect, "<size=20>低(R)</size>"))
            // {
            //     bool isSuccess = gmInstance.SetResolutionLevel(GraphicManager.ResolutionLevel.Low);
            //     LogSetResult(isSuccess);
            // }
        }
    }

    // Update和Time.deltaTime的时间取值不一样,FixedUpdate
    void Update()
    {
        ++frames;
        float newSample = Time.realtimeSinceStartup;
        float deltaTime = newSample - lastSample;
        lastSample = newSample;
        timeleft -= deltaTime;
        accum += 1.0f / deltaTime;
        if (timeleft <= 0.0f)
        {
            fps = accum / frames;
            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
            ++wholeGotIntervals;
        }
    }
}