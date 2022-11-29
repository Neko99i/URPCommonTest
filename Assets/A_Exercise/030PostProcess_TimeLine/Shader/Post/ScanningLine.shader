Shader "LLY/ScanningLine"
{
    Properties
    {
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
         [HDR]_ColorX("ColorX",Color)=(1,1,1,1)

        [HDR]_ColorY("ColorY",Color)=(1,1,1,1)

        [HDR]_ColorZ("ColorZ",Color)=(1,1,1,1)

        [HDR]_EdgeColor("_EdgeColor",Color)=(1,1,1,1)

        [HDR]_OutLineColor("OutLineColor",Color)=(1,1,1,1)

        _Width("Width",Range(0,1))=0.02

        _Spacing("Spacing",float)=1

        _Speed("Speed",float)=1

        _SampleScale("SampleScale",float)=1

        _Sensitivity("_Sensitivity",Vector)=(1,1,1,1)

        [KeywordEnum(X,Y,Z)]_AXIS("Axis",float)=1
    }
    SubShader
    {
       Tags { "RenderPipeline"="UniversalPipeline"}
        Cull Off 
        ZTest Always
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            #pragma  multi_compile_local _AXIS_X _AXIS_Y _AXIS_Z 

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv_Depth : TEXCOORD1;
                float4 pos : SV_POSITION;
                float4 interpolatedRay:TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)

            float4 _MainTex_TexelSize;

            float4x4 _FrustumCornersRay;

            float _SampleScale;
            half4 _Sensitivity;
            
            half4 _ColorX;
            half4 _ColorY;
            half4 _ColorZ;
            half4 _EdgeColor;
            half4 _OutLineColor;

            float _Width;
            float _Spacing;
            float _Speed;

            CBUFFER_END

            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);

            sampler2D _CameraDepthNormalsTexture;
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            v2f vert (appdata v)
            {
                v2f o;
                o.pos=TransformObjectToHClip(v.vertex.xyz);
                o.uv=v.uv;
                o.uv_Depth=v.uv;

                int index=0;
                if (v.uv.x<0.5 && v.uv.y<0.5)
                {
                    index=0;
                }
                else if(v.uv.x>0.5 && v.uv.y<0.5)
                {
                  index=1;
                }
                else if(v.uv.x<0.5 && v.uv.y>0.5)
                {
                    index=2;
                }
                else
                {
                    index=3;
                }

                #if UNITY_UV_STARTS_AT_TOP
                if(_MainTex_TexelSize.y<0)
                {
                    o.uv_Depth.y=1-o.uv_Depth.y;
                    // 这里需要把图片倒过来看
                    index=3-index;
                }
                #endif

                o.interpolatedRay=_FrustumCornersRay[index];
                return o;
            }

    //Roberts算子实际是计算两个对角差值后与阈值对比判断是否算边界 
    half CheckSame_Roberts(half4 sample1 ,half4 sample2 )
    {   
        //这里我们只需要比较法线差值不需要解出来
        half2 s1Normal=sample1.xy;
        half s1Depth=sample1.zw;

        half2 s2Normal=sample2.xy;
        half s2Depth=sample2.zw;

        //法线差值 * 灵敏度
        half2 diffNormal=abs(s1Normal-s2Normal) *_Sensitivity.x;
        //返回0/1
        int normalSame=(diffNormal.x+diffNormal.y) < 0.1;

        //深度差值 * 灵敏度
        float diffDepth=abs(s1Depth-s2Depth) *_Sensitivity.y;
        int depthSame = diffDepth < 0.1;

        return normalSame * depthSame ? 1 : 0;
    }
            
            //解法线
            float3 DecodeViewNormal(float4 depthColor)
            {
                float kscale=1.7777f;
                float3 nn = depthColor.xyz*float3(2*kscale,2*kscale,0) + float3(-kscale,-kscale,1);
                float g=2/dot(nn.xyz,nn.xyz);
                float3 n;
                n.xy=g*nn.xy;
                n.z=g-1;
                return n;
            }
            //解深度
            float DecodeViewDepth(float4 depthColor)
            {
               return (depthColor.z *1.0+depthColor.w/255.0)*_ProjectionParams.z ;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 sample1 =tex2D(_CameraDepthNormalsTexture,i.uv_Depth+ _MainTex_TexelSize.xy * half2(1,1) *_SampleScale);
                half4 sample2 =tex2D(_CameraDepthNormalsTexture,i.uv_Depth+ _MainTex_TexelSize.xy * half2(-1,-1) *_SampleScale);
                half4 sample3 =tex2D(_CameraDepthNormalsTexture,i.uv_Depth+ _MainTex_TexelSize.xy * half2(-1,1) *_SampleScale);
                half4 sample4 =tex2D(_CameraDepthNormalsTexture,i.uv_Depth+ _MainTex_TexelSize.xy * half2(1,-1) *_SampleScale);

                half edge=1;
                edge*= CheckSame_Roberts(sample1,sample2);
                edge*= CheckSame_Roberts(sample3,sample4);
                // return (1-edge)*_EdgeColor;

                half4 depthNormal=tex2D(_CameraDepthNormalsTexture,i.uv_Depth);
                
                float viewDepth= DecodeViewDepth(depthNormal) ;
                // float depth01= depthNormal.z*1.0+depthNormal.w/255.0;//得到01线性的深度
                float3 viewNormal=DecodeViewNormal(depthNormal);

                half4 tex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);

                // 下面这个深度需要/_ProjecttionParams.z才是线性深度值
                // float depth= LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,sampler_CameraDepthTexture,i.uv)  ,_ZBufferParams).x;
                float3 worldPos=_WorldSpaceCameraPos+ i.interpolatedRay.xyz * viewDepth;

                //扫光
                #ifdef _AXIS_X
                    //在X轴方向计算mask
                    float mask=saturate(pow(abs(frac(worldPos.x+_Time.y*0.1*_Speed)-0.75),10)*30);
                    mask+=step(0.999,mask);
                #elif _AXIS_Y
                    //在Y轴方向计算mask
                    float mask=saturate(pow(abs(frac(worldPos.y-_Time.y*0.1*_Speed)-0.25),10)*30);
                    mask+=step(0.999,mask);
                #elif _AXIS_Z
                    //在Z轴方向计算mask
                    float mask=saturate(pow(abs(frac(worldPos.z+_Time.y*0.1*_Speed)-0.75),10)*30);
                    mask+=step(0.999,mask);
                #endif
                // return mask;
                
                float3 Line=step(1-_Width,frac(worldPos/_Spacing));//线框

                float4 Linecolor=Line.x*_ColorX+Line.y*_ColorY+Line.z*_ColorZ + (1-edge)*_OutLineColor;
                //给线框上色
                return lerp(tex,Linecolor+_EdgeColor,mask);
            }
            ENDHLSL
        }
    }
}
