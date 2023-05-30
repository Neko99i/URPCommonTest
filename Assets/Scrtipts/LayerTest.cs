using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;

public class LayerTest : MonoBehaviour
{
    
    public LayerMask l;
    public int i=1;

    [Button]
    public void IntToName()
    {
      string name =  LayerMask.LayerToName(i);
      Debug.Log(name);
    }
    
    [Button]
    public void IsContainLayer()
    {
        bool isContain= ((1 << gameObject.layer) & l) > 0;
        Debug.Log(isContain);
    }
    
    [Button]
    public void GetlayerValue()
    {
        int a =  l.value;
        Debug.Log(a);
    }
}
