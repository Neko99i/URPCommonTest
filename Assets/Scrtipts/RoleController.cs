using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class RoleController : MonoBehaviour
{
    public Vector4 _PlaneShodwY = new Vector4(0, 0, 0, 0);

    private void Update()
    {
        float x = _PlaneShodwY.x;
        float y = _PlaneShodwY.y;
        float z = _PlaneShodwY.z;
        float w = transform.position.y + _PlaneShodwY.w;
        Renderer[] rs = gameObject.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < rs.Length; i++)
        {
            Material mat = rs[i].sharedMaterial;
            if (!mat)
                continue;
            mat.SetFloat("_PlaneShodwY", w);
        }
    }
}