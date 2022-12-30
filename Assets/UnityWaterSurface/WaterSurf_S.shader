Shader "Water/Surface"
{

Properties
{
    _Color("Color", color) = (1, 1, 0, 0)
    _ColorNeg("ColorNeg", color) = (0, 0.3, 1, 0)
    _DispTex("Disp Texture", 2D) = "gray" {}
    _Glossiness ("Smoothness", Range(0,1)) = 0.5
    _Metallic ("Metallic", Range(0,1)) = 0.0
    _MinDist("Min Distance", Range(0.1, 50)) = 10
    _MaxDist("Max Distance", Range(0.1, 50)) = 25
    _TessFactor("Tessellation", Range(1, 50)) = 10
    _Displacement("Displacement", Range(0, 1.0)) = 0.3
}

SubShader
{

Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

CGPROGRAM

#pragma surface surf Standard alpha addshadow fullforwardshadows vertex:disp tessellate:tessDistance
#pragma target 5.0
#include "Tessellation.cginc"

float _TessFactor;
float _Displacement;
float _MinDist;
float _MaxDist;
sampler2D _DispTex;
float4 _DispTex_TexelSize;
fixed4 _Color;
fixed4 _ColorNeg;
half _Glossiness;
half _Metallic;

struct appdata 
{
    float4 vertex   : POSITION;
    float4 tangent  : TANGENT;
    float3 normal   : NORMAL;
    float2 texcoord : TEXCOORD0;
};

struct Input 
{
    float2 uv_DispTex;
};

float4 tessDistance(appdata v0, appdata v1, appdata v2) 
{
    return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, _MinDist, _MaxDist, _TessFactor);
}

void disp(inout appdata v)
{
    float d = tex2Dlod(_DispTex, float4(v.texcoord.xy, 0, 0)).r * _Displacement;
    v.vertex.xyz += v.normal * d;
}

void surf(Input IN, inout SurfaceOutputStandard o) 
{
    float val = (tex2D(_DispTex, IN.uv_DispTex).r+1)*0.5;
    o.Albedo = lerp(_ColorNeg.rgb,_Color.rgb,val);
    o.Metallic = _Metallic;
    o.Smoothness = _Glossiness;
    o.Alpha = 1;// val > 0 ? _Color.a * clamp(val, 0.2, 1) : _ColorNeg.a * clamp(-val, 0.2, 1);


    float3 duv = float3(_DispTex_TexelSize.xy, 0);
    half v1 = tex2D(_DispTex, IN.uv_DispTex - duv.xz).y;
    half v2 = tex2D(_DispTex, IN.uv_DispTex + duv.xz).y;
    half v3 = tex2D(_DispTex, IN.uv_DispTex - duv.zy).y;
    half v4 = tex2D(_DispTex, IN.uv_DispTex + duv.zy).y;
    o.Normal = normalize(float3(v1 - v2, v3 - v4, 0.3));
}

ENDCG

}

FallBack "Diffuse"

}