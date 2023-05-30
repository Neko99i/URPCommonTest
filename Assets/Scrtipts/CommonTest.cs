using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class CommonTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Button]
    public void PrintSWH()
    {
        Debug.Log(string.Format("Sceeen01:{0}--{1}",  Screen.width ,Screen.height));
        Debug.Log(string.Format("Sceeen02:{0}--{1}",  Screen.currentResolution.width ,Screen.currentResolution.height));
        Debug.Log(string.Format("Sceeen03:{0}--{1}",  UnityEditor.Handles.GetMainGameViewSize().x ,UnityEditor.Handles.GetMainGameViewSize().y));
    }
}
