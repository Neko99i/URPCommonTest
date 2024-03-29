Shader "Hidden/Shader Forge/SFN_ConstantClamp" {
    Properties {
        _OutputMask ("Output Mask", Vector) = (1,1,1,1)
        _IN ("", 2D) = "black" {}
        _clampmin ("ClampMin", Float) = 0 
        _clampmax ("ClampMax", Float) = 0 
    }
    SubShader {
        Tags {
            "RenderType"="Opaque"
        }
        Pass {
        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define UNITY_PASS_FORWARDBASE
            #include "UnityCG.cginc"
            #pragma target 3.0
            uniform float4 _OutputMask;
            uniform sampler2D _IN;
            uniform float _clampmin;
            uniform float _clampmax;

            struct VertexInput {
                float4 vertex : POSITION;
                float2 texcoord0 : TEXCOORD0;
            };
            struct VertexOutput {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            VertexOutput vert (VertexInput v) {
                VertexOutput o = (VertexOutput)0;
                o.uv = v.texcoord0;
                o.pos = UnityObjectToClipPos(v.vertex );
                return o;
            }
            float4 frag(VertexOutput i) : COLOR {

                // Read inputs
                float4 _in = tex2D( _IN, i.uv );

                // Operator
                float4 outputColor = clamp( _in, _clampmin, _clampmax );

                // Return
                return outputColor * _OutputMask;
            }
            ENDHLSL
        }
    }
}
