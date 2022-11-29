using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways]
public class RenderMatcap : MonoBehaviour
{
    void Update()
    {
        transform.localRotation = Camera.main.transform.localRotation;
    }
}
