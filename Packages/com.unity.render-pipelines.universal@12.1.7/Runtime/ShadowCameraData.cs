using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


namespace UnityEngine.Rendering.Universal
{
    [RequireComponent(typeof(Light))]
    [RequireComponent(typeof(Camera))]
    [ExecuteAlways]
    public class ShadowCameraData : MonoBehaviour
    {
        private Camera m_shadowcamera;

        private Camera shadowCamera
        {
            get
            {
                if (m_shadowcamera == null)
                {
                    m_shadowcamera = this.GetComponent<Camera>();
                }

                return m_shadowcamera;
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

        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            shadowCamera.orthographic = true;
            shadowCamera.backgroundColor = Color.black;
            shadowCamera.clearFlags = CameraClearFlags.Color;
            m_viewMatrix = shadowCamera.worldToCameraMatrix;
            m_projMatrix = shadowCamera.projectionMatrix;
        }
    }
}