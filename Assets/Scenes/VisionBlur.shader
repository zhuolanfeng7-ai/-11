Shader "Hidden/VisionBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _BlurDirection;

            fixed4 frag(v2f_img input) : SV_Target
            {
                float2 uv = input.uv;
                float2 stepUV = _BlurDirection.xy;

                fixed4 color = tex2D(_MainTex, uv) * 0.227027;
                color += tex2D(_MainTex, uv + stepUV * 1.384615) * 0.316216;
                color += tex2D(_MainTex, uv - stepUV * 1.384615) * 0.316216;
                color += tex2D(_MainTex, uv + stepUV * 3.230769) * 0.070270;
                color += tex2D(_MainTex, uv - stepUV * 3.230769) * 0.070270;
                return color;
            }
            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment fragGrayscale
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Saturation;

            fixed4 fragGrayscale(v2f_img input) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, input.uv);
                float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
                color.rgb = lerp(gray.xxx, color.rgb, saturate(_Saturation));
                return color;
            }
            ENDCG
        }
    }

    Fallback Off
}
