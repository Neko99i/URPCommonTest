Shader "LLY/SRP/LitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainColor ("MainColor", Color) = (1,1,1,1)
    }
    SubShader
    {
      
        Pass
        { 
            Tags{"LightMode"="CustomMode"}

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile CUSTOM_COMMON_INCLUDED
            #include "LitPass.hlsl"

            ENDHLSL
        }
    }
}
