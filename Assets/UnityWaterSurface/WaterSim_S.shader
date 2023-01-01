﻿Shader "Water/Simulation"
{

    Properties
    {
        _CFLSq("CFL^2", Range(0.0, 0.5)) = 0.2
        _Effect("Effect",Vector) = (0,0,0,0)
        _CFAbsorb("CFAbsorb", Range(0.0,1.0)) = 0.2
    }

        CGINCLUDE

#include "UnityCustomRenderTexture.cginc"

    half _CFLSq;
    float4 _Effect;
    float _CFAbsorb;

float4 frag(v2f_customrendertexture i) : SV_Target
{
    float2 uv = i.globalTexcoord;

    float du = 1.0 / _CustomRenderTextureWidth;
    float dv = 1.0 / _CustomRenderTextureHeight;
    float3 duv = float3(du, dv, 0);
    int xPixel = (int)(floor(uv.x * _CustomRenderTextureWidth));
    int yPixel = (int)(floor(uv.y * _CustomRenderTextureHeight));
    int atLeft = (int)(xPixel <= 0);
    int atRight = (int)(xPixel >= (_CustomRenderTextureWidth-1));
    int atBottom = (int)(xPixel <= 0);
    int atTop = (int)(xPixel >= (_CustomRenderTextureHeight - 1));

    float2 c = tex2D(_SelfTexture2D, uv);
    float p = (2 * c.r - c.g + _CFLSq * (
        tex2D(_SelfTexture2D, uv - duv.zy).r +
        tex2D(_SelfTexture2D, uv + duv.zy).r +
        tex2D(_SelfTexture2D, uv - duv.xz).r +
        tex2D(_SelfTexture2D, uv + duv.xz).r - 4 * c.r));
    float effectDelta = clamp(abs(xPixel - floor(_Effect.x)), 0, 1);

    p = lerp(_Effect.z, p, effectDelta);
    return float4(p, c.r, 0, 0);
}

float4 frag_left_click(v2f_customrendertexture i) : SV_Target
{
    return float4(-1, 0, 0, 0);
}

float4 frag_right_click(v2f_customrendertexture i) : SV_Target
{
    return float4(1, 0, 0, 0);
}

ENDCG

SubShader
{
    Cull Off ZWrite Off ZTest Always

    Pass
    {
        Name "Update"
        CGPROGRAM
        #pragma vertex CustomRenderTextureVertexShader
        #pragma fragment frag
        ENDCG
    }

    Pass
    {
        Name "LeftClick"
        CGPROGRAM
        #pragma vertex CustomRenderTextureVertexShader
        #pragma fragment frag_left_click
        ENDCG
    }

    Pass
    {
        Name "RightClick"
        CGPROGRAM
        #pragma vertex CustomRenderTextureVertexShader
        #pragma fragment frag_right_click
        ENDCG
    }
}

}