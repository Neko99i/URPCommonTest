Shader "Tinynine/OutLine"
{
    Properties
    {
        _OutLineColor("LineColor",COLOR)=(1,1,1,1)
        _NormalOff("NormalOff",Range(0,1))=0.5
        _Factor("Factor", Range(0,1)) = 0.1
    }
    SubShader
    {
        Pass
        {
            Name "OUTLINE"     
            Cull Front
            ZWrite on
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"
             #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal:NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            half3 _OutLineColor;
            float _NormalOff , _Factor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 N_view=mul((float3x3)(UNITY_MATRIX_IT_MV),v.normal);
                // viewNormal.z= - _NormalOff;
                float2 offset=TransformViewToProjection(N_view.xy);
                o.pos.xy += offset * _Factor * 0.1 * o.pos.z;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                return half4(_OutLineColor,1) ;
            }
            ENDHLSL
        }
    }
}
