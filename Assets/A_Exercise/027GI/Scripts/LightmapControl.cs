using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LightmapControl : MonoBehaviour
{
    MeshRenderer mr;
    public int index = 0;
    void Start()
    {
        mr = this.GetComponent<MeshRenderer>();
   
    }

   
    void Update()
    {
        mr.lightmapIndex = index;
    }
}
