Shader "Unlit/SimpleVF"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor ("Color", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcMode("SrcMode",float)=1
        [Enum(UnityEngine.Rendering.BlendMode)] _Dstode("Dstode",float)=0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
       
        Pass
        {
            Blend [_SrcMode] [_Dstode]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            //GPU实例
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
           
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;

            // CBUFFER_START(UnityPerMaterial)
            //     half4 _MainColor;
            //     float4 _MainTex_ST;
            // CBUFFER_END
            
            //GPU实例化
            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

                UNITY_DEFINE_INSTANCED_PROP(float4, _MainColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _MainTex_ST)

            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


            // CBUFFER_START(UnityPerDraw)
	        //     float4x4 unity_ObjectToWorld;
	        //     float4x4 unity_WorldToObject;
	        //     float4 unity_LODFade;
	        //     real4 unity_WorldTransformParams;
            // CBUFFER_END
            
            v2f vert (appdata v)
            {
                v2f o;
                //它将从顶点数据中提取索引并将其存储在其它实例化宏所依赖的全局静态变量中。
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);

                float4 MainTex_ST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
	             o.uv = v.uv * MainTex_ST.xy + MainTex_ST.zw;
                // o.uv=v.uv;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                half4 tex=tex2D(_MainTex,i.uv)*UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainColor);

                // return tex;
	            return tex;

            }

            ENDHLSL
        }
    }
}
