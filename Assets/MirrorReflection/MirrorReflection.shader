// original source from: http://wiki.unity3d.com/index.php/MirrorReflection4
// taken from: https://forum.unity.com/threads/mirror-reflections-in-vr.416728/#post-3594431

Shader "FX/MirrorReflection"
{
    Properties
    {
        _MainTex ("_MainTex", 2D) = "white" {}
        _ReflectionTexLeft ("_ReflectionTexLeft", 2D) = "white" {}
        _ReflectionTexRight ("_ReflectionTexRight", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 refl : TEXCOORD1;
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            float4 _MainTex_ST;
            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.pos = UnityObjectToClipPos (v.pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.refl = ComputeScreenPos (o.pos);
                return o;
            }
            sampler2D _MainTex;
            //UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
            sampler2D _ReflectionTexLeft;
            sampler2D _ReflectionTexRight;
            //UNITY_DECLARE_SCREENSPACE_TEXTURE(_ReflectionTexLeft);
            //UNITY_DECLARE_SCREENSPACE_TEXTURE(_ReflectionTexRight);
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                fixed4 tex = tex2D(_MainTex, i.uv);
                //fixed4 tex = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.uv);
                fixed4 refl;

                //ピンクになる
                if (unity_StereoEyeIndex == 0) refl = tex2Dproj(_ReflectionTexLeft, UNITY_PROJ_COORD(i.refl));
                else refl = tex2Dproj(_ReflectionTexRight, UNITY_PROJ_COORD(i.refl));

                //ピンクになる
                //if (unity_StereoEyeIndex == 0) refl = UNITY_SAMPLE_TEX2DARRAY(_ReflectionTexLeft, float3((i.refl).x / (i.refl).w, (i.refl).y / (i.refl).w, (float)unity_StereoEyeIndex));
                //else refl = UNITY_SAMPLE_TEX2DARRAY(_ReflectionTexRight, float3((i.refl).x / (i.refl).w, (i.refl).y / (i.refl).w, (float)unity_StereoEyeIndex));
                
                /*
                //fixed4 rpos = UNITY_PROJ_COORD(i.refl);
                fixed4 rpos = i.refl;
                if (unity_StereoEyeIndex == 0) {
                    refl = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ReflectionTexLeft,rpos.xy/rpos.w);
                    //refl = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ReflectionTexLeft, rpos.xy);
                }
                else {
                    refl = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ReflectionTexRight, rpos.xy / rpos.w);
                    //refl = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_ReflectionTexRight, rpos.xy);
                }
                */

                /*
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
                if (unity_StereoEyeIndex == 0) refl = UNITY_SAMPLE_TEX2DARRAY(_ReflectionTexLeft, float3((i.refl).x/(i.refl).w, (i.refl).y/(i.refl).w, (float)unity_StereoEyeIndex));
                else refl = UNITY_SAMPLE_TEX2DARRAY(_ReflectionTexRight, float3((i.refl).x / (i.refl).w, (i.refl).y / (i.refl).w, (float)unity_StereoEyeIndex));
#else
                if (unity_StereoEyeIndex == 0) refl = tex2Dproj(_ReflectionTexLeft, UNITY_PROJ_COORD(i.refl));
                else refl = tex2Dproj(_ReflectionTexRight, UNITY_PROJ_COORD(i.refl));
#endif
                */
                return tex * refl;
                
            }
            ENDCG
        }
    }
}