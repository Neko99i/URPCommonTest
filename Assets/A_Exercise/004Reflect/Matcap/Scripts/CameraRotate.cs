using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class CameraRotate : MonoBehaviour
{
    public Transform target;
    [Range(0,5)]
    public float rotateSpeed=2.0f;
    void Update()
    {
        if(target)
        transform.RotateAround(target.position, Vector3.up, rotateSpeed);
    }
}
