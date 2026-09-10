Shader "Custom/TVScreenGlitch"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _UVRotation ("UV Rotation", Float) = 0
        _ScanlineStrength ("Scanline Strength", Range(0,1)) = 0.16
        _FlickerStrength ("Brightness Flicker", Range(0,0.15)) = 0.028
        _RgbOffset ("RGB Separation", Range(0,0.02)) = 0.0022
        _ScanlineSpeed ("Scanline Speed", Float) = 0.65
        _DisplayTime ("Display Time", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _UVRotation;
            float _ScanlineStrength;
            float _FlickerStrength;
            float _RgbOffset;
            float _ScanlineSpeed;
            float _DisplayTime;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float radiansValue = radians(_UVRotation);
                float sineValue = sin(radiansValue);
                float cosineValue = cos(radiansValue);
                float2 centred = input.uv - 0.5;
                float2 rotated = float2(
                    centred.x * cosineValue - centred.y * sineValue,
                    centred.x * sineValue + centred.y * cosineValue);
                float2 uv = rotated + 0.5;
                // Restrained LCD/CRT character: fine scanlines, gentle brightness
                // drift, and sub-pixel colour separation.  The poster stays readable.
                fixed4 baseColor = tex2D(_MainTex, uv);
                float red = tex2D(_MainTex, uv + float2(_RgbOffset, 0)).r;
                float blue = tex2D(_MainTex, uv - float2(_RgbOffset, 0)).b;
                baseColor.r = red;
                baseColor.b = blue;
                float scan = sin((uv.y * 720.0 + _DisplayTime * _ScanlineSpeed * 16.0) * 3.14159265) * .5 + .5;
                float lineMultiplier = 1.0 - scan * _ScanlineStrength;
                float flicker = 1.0 + sin(_DisplayTime * 7.73) * _FlickerStrength
                    + sin(_DisplayTime * 2.19) * (_FlickerStrength * .45);
                baseColor.rgb *= lineMultiplier * flicker;
                return baseColor * _Color;
            }
            ENDCG
        }
    }
}
