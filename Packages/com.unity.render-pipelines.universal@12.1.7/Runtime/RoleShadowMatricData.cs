using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    [ExecuteAlways]
    [RequireComponent(typeof(Light))]
    public class RoleShadowMatricData : MonoBehaviour
    {
        #region Shadow

        // Start is called before the first frame update
        public float nearOffset = 0;
        public float farOffset = 0;

        [OnValueChanged("UpdateSkinmeshes")] public LayerMask layers;

        //可以升级为很多个transform

        private Light m_mainLight;

        private Light mainLight
        {
            get
            {
                if (m_mainLight == null)
                    m_mainLight = this.GetComponent<Light>();
                return m_mainLight;
            }
        }

        private Matrix4x4 m_viewMatrix, m_projMatrix;

        public Matrix4x4 viewMatrix
        {
            get { return m_viewMatrix; }
        }

        public Matrix4x4 projMatrix
        {
            get { return m_projMatrix; }
        }

        private Vector3[] vertexPositions = new Vector3[8];

        private int boundsCount;

        private Vector3 cameraCenterPos = Vector3.zero;
        
        private Camera viewCamera = null;

        private List<Vector3> cornersList = new List<Vector3>();
        
        private Bounds bounds = new Bounds();

        private List<SkinnedMeshRenderer> m_skinmeshes = new List<SkinnedMeshRenderer>();

        public List<SkinnedMeshRenderer> skinmeshes
        {
            get
            {
                UpdateSkinmeshes();
                return m_skinmeshes;
            }
        }

        private void UpdateSkinmeshes()
        {
            m_skinmeshes.Clear();

            SkinnedMeshRenderer[] skinnedMeshRenderers =
                Resources.FindObjectsOfTypeAll(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer[];

            foreach (var renderer in skinnedMeshRenderers)
            {
                if (renderer != null && renderer.gameObject.activeInHierarchy &&
                    IsContainLayer(renderer.gameObject, layers))
                {
                    m_skinmeshes.Add(renderer);
                }
            }

            if (m_skinmeshes.Count == 0)
                Debug.Log("选择需要高精度阴影的物体！");
        }

        private void OnEnable()
        {
            // CreateViewCamera();
        }

        void Start()
        {
            UpdateAlldata();
        }

        private void CreateViewCamera()
        {
            GameObject lightCamObj = new GameObject("DepthCamera");
            lightCamObj.hideFlags = HideFlags.HideAndDontSave;
            lightCamObj.SetActive(false);
            viewCamera = lightCamObj.AddComponent<Camera>();
            viewCamera.orthographic = true;
            viewCamera.backgroundColor = Color.black;
            viewCamera.clearFlags = CameraClearFlags.Color;
        }

        private void UpdateCamera(Vector3 cameraPos,float deltaX, float deltaY, float deltaZ, float aspect)
        {
            if (!viewCamera)
                return;
            
            viewCamera.transform.rotation = mainLight.transform.rotation;
            viewCamera.transform.forward = mainLight.transform.forward;
            viewCamera.transform.position = cameraPos;

            viewCamera.nearClipPlane = 0;
            viewCamera.farClipPlane = deltaZ;
            viewCamera.orthographicSize = deltaY / 2; //orthographicSize是高的一半
            viewCamera.aspect = aspect;
        }

        private List<Vector3> GetCameraCornersWS(Matrix4x4 pm, Matrix4x4 vm)
        {
            var inv = Matrix4x4.Inverse(pm * vm);
            List<Vector3> cornersList = new List<Vector3>();
            for (int x = 0; x < 2; ++x)
            {
                for (int y = 0; y < 2; ++y)
                {
                    for (int z = 0; z < 2; ++z)
                    {
                        Vector3 pt =
                            inv * new Vector4(
                                2.0f * x - 1.0f,
                                2.0f * y - 1.0f,
                                2.0f * z - 1.0f,
                                1.0f);
                        cornersList.Add(pt);
                    }
                }
            }

            return cornersList;
        }

        private void OnDisable()
        {
            if (viewCamera.gameObject)
                DestroyImmediate(viewCamera.gameObject);
        }

        private void Update()
        {
            UpdateAlldata();
        }

        private void CalculateAABB(int boundsCount, SkinnedMeshRenderer skinmeshRender)
        {
            if (boundsCount != 0)
                bounds.Encapsulate(skinmeshRender.bounds);
            else
                bounds = skinmeshRender.bounds;
        }

        private void UpdateAlldata()
        {
            cornersList.Clear();

            UpdateAABB();
            fitToScene();
        }

        private void UpdateAABB()
        {
            if (skinmeshes == null || skinmeshes.Count == 0)
                return;

            int boundscount = 0;
            foreach (var skinmesh in skinmeshes)
            {
                CalculateAABB(boundscount, skinmesh);
                boundscount += 1;
            }

            float x = bounds.extents.x; //范围这里是三维向量，分别取得X Y Z
            float y = bounds.extents.y;
            float z = bounds.extents.z;

            vertexPositions[0] = new Vector3(x, y, z) + bounds.center;
            vertexPositions[1] = new Vector3(x, -y, z) + bounds.center;
            vertexPositions[2] = new Vector3(x, y, -z) + bounds.center;
            vertexPositions[3] = new Vector3(x, -y, -z) + bounds.center;
            vertexPositions[4] = new Vector3(-x, y, z) + bounds.center;
            vertexPositions[5] = new Vector3(-x, -y, z) + bounds.center;
            vertexPositions[6] = new Vector3(-x, y, -z) + bounds.center;
            vertexPositions[7] = new Vector3(-x, -y, -z) + bounds.center;
        }

        private void fitToScene()
        {
            float xmin = float.MaxValue, xmax = float.MinValue;
            float ymin = float.MaxValue, ymax = float.MinValue;
            float zmin = float.MaxValue, zmax = float.MinValue;

            foreach (var vertex in vertexPositions)
            {
                Vector3 vertexLS = mainLight.transform.worldToLocalMatrix.MultiplyPoint(vertex);
                xmin = Mathf.Min(xmin, vertexLS.x);
                xmax = Mathf.Max(xmax, vertexLS.x);
                ymin = Mathf.Min(ymin, vertexLS.y);
                ymax = Mathf.Max(ymax, vertexLS.y);
                zmin = Mathf.Min(zmin, vertexLS.z);
                zmax = Mathf.Max(zmax, vertexLS.z);
            }

            m_viewMatrix = mainLight.transform.worldToLocalMatrix;


#if UNITY_EDITOR
            m_viewMatrix.m20 = -m_viewMatrix.m20;
            m_viewMatrix.m21 = -m_viewMatrix.m21;
            m_viewMatrix.m22 = -m_viewMatrix.m22;
            m_viewMatrix.m23 = -m_viewMatrix.m23;
#else
            if (SystemInfo.usesReversedZBuffer)
            {
                m_viewMatrix.m20 = -m_viewMatrix.m20;
                m_viewMatrix.m21 = -m_viewMatrix.m21;
                m_viewMatrix.m22 = -m_viewMatrix.m22;
                m_viewMatrix.m23 = -m_viewMatrix.m23;
            }
#endif


            zmax += farOffset;
            zmin += nearOffset;

            Vector4 row0 = new Vector4(2 / (xmax - xmin), 0, 0, -(xmax + xmin) / (xmax - xmin));
            Vector4 row1 = new Vector4(0, 2 / (ymax - ymin), 0, -(ymax + ymin) / (ymax - ymin));
            Vector4 row2 = new Vector4(0, 0, -2 / (zmax - zmin), -(zmax + zmin) / (zmax - zmin));
            Vector4 row3 = new Vector4(0, 0, 0, 1);

            m_projMatrix.SetRow(0, row0);
            m_projMatrix.SetRow(1, row1);
            m_projMatrix.SetRow(2, row2);
            m_projMatrix.SetRow(3, row3);

            cornersList = GetCameraCornersWS(m_projMatrix, m_viewMatrix);
            
            Vector3 sumV = Vector3.zero;
            for (int i = 0; i < cornersList.Count; i++)
            {
                sumV += cornersList[i];
            }
            Vector3 centerPos = sumV / cornersList.Count;
            cameraCenterPos = centerPos - mainLight.transform.forward * (zmax - zmin) / 2;
            
            UpdateCamera(cameraCenterPos, xmax - xmin, (ymax - ymin), zmax - zmin, (xmax - xmin) / (ymax - ymin));
        }

        public bool IsContainLayer(GameObject go, LayerMask layers)
        {
            return ((1 << go.layer) & layers) > 0;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // var rs = gameObject.GetComponentsInChildren<Renderer>();
            //
            // Gizmos.color = Color.blue;
            // Gizmos.matrix = Matrix4x4.identity;
            //
            // foreach (var r in rs)
            // {
            //     if (r == null)
            //         continue;
            //     Bounds bounds = r.bounds;
            //     Gizmos.DrawWireCube(bounds.center, bounds.extents * 2);
            // }
        }

        private void OnDrawGizmos()
        {
            if (cornersList == null || cornersList.Count == 0)
                return;

            Recangle3D recangle3D = new Recangle3D();

            for (int i = 0; i < cornersList.Count; i++)
            {
                Vector3 pos = cornersList[i];
                if (pos != null)
                {
                    DrawIndexID(pos, i + 1);
                }
            }

            DrawCameraIcon(cameraCenterPos);
            
            recangle3D.point01 = cornersList[0];
            recangle3D.point02 = cornersList[1];
            recangle3D.point03 = cornersList[3];
            recangle3D.point04 = cornersList[2];

            recangle3D.point05 = cornersList[4];
            recangle3D.point06 = cornersList[5];
            recangle3D.point07 = cornersList[7];
            recangle3D.point08 = cornersList[6];
            DrawRecangle3D(recangle3D);

            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private string texIdPath = "Assets/A_Exercise/033CustomShadow/RoleShadow/Gizmos/";

        private void DrawIndexID(Vector3 centerPos, int index)
        {
            Gizmos.DrawSphere(centerPos, 0.02f);
            Gizmos.DrawIcon(centerPos, $"{texIdPath}Index_{index}.tga");
        }

        private void DrawCameraIcon(Vector3 centerPos)
        {
            Gizmos.DrawIcon(centerPos, $"{texIdPath}cameraIcon.tga");
        }

        
        private void DrawRecangle3D(Recangle3D recangle3D)
        {
            Gizmos.color = Color.cyan;

            Gizmos.DrawLine(recangle3D.point01, recangle3D.point02);
            Gizmos.DrawLine(recangle3D.point01, recangle3D.point04);
            Gizmos.DrawLine(recangle3D.point01, recangle3D.point05);

            Gizmos.DrawLine(recangle3D.point03, recangle3D.point02);
            Gizmos.DrawLine(recangle3D.point03, recangle3D.point04);
            Gizmos.DrawLine(recangle3D.point03, recangle3D.point07);


            Gizmos.DrawLine(recangle3D.point06, recangle3D.point02);
            Gizmos.DrawLine(recangle3D.point06, recangle3D.point05);
            Gizmos.DrawLine(recangle3D.point06, recangle3D.point07);


            Gizmos.DrawLine(recangle3D.point08, recangle3D.point04);
            Gizmos.DrawLine(recangle3D.point08, recangle3D.point05);
            Gizmos.DrawLine(recangle3D.point08, recangle3D.point07);
        }

        #endregion
    }

    public class Recangle3D
    {
        public Vector3 point01;
        public Vector3 point02;
        public Vector3 point03;
        public Vector3 point04;
        public Vector3 point05;
        public Vector3 point06;
        public Vector3 point07;
        public Vector3 point08;
    }

    public class CameraVolumBoxData
    {
        public Vector3 center;
        public Vector3 size;
    }
}