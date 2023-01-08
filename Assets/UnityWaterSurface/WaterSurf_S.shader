Shader "Water/Surface"
{

Properties
{
    _Color("Color", color) = (1, 1, 0, 0)
    _ColorNeg("ColorNeg", color) = (0, 0.3, 1, 0)
    _ViewSelection("Show A=0, A^2=1, E=2",Range(0.0,2.0)) = 0.0    
    _DispTex("Disp Texture", 2D) = "gray" {}
    _Glossiness("Smoothness", Range(0,1)) = 0.5
    _Metallic("Metallic", Range(0,1)) = 0.0
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

float _ViewSelection;
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
    float d = tex2Dlod(_DispTex, float4(v.texcoord.xy, 0, 0)).z * _Displacement;
    v.vertex.xyz += v.normal * d;
}

void surf(Input IN, inout SurfaceOutputStandard o) 
{
    o.Metallic = _Metallic;
    o.Smoothness = _Glossiness;
    o.Alpha = 1;
    int showAmpSquared = (int)((_ViewSelection > 0.5) && (_ViewSelection < 1.5));
    int showEnergy = (int)(_ViewSelection > 1.5);

    float3 duv = float3(_DispTex_TexelSize.xy, 0);
    float val;
    float v1;
    float v2;
    float v3;
    float v4;
    float range;
    if (showEnergy < 1)
    {
        val = tex2D(_DispTex, IN.uv_DispTex).r;
        v1 = tex2D(_DispTex, IN.uv_DispTex - duv.xz).r;
        v2 = tex2D(_DispTex, IN.uv_DispTex + duv.xz).r;
        v3 = tex2D(_DispTex, IN.uv_DispTex - duv.zy).r;
        v4 = tex2D(_DispTex, IN.uv_DispTex + duv.zy).r;
        range = (val + 1) * 0.5;
        if (showAmpSquared == 1)
        {
            val *= val;
            v1 *= v1;
            v2 *= v2;
            v3 *= v3;
            v4 *= v4;
            range =  val;

        }
    }
    else
    {
        val = tex2D(_DispTex, IN.uv_DispTex).b;
        v1 = tex2D(_DispTex, IN.uv_DispTex - duv.xz).b;
        v2 = tex2D(_DispTex, IN.uv_DispTex + duv.xz).b;
        v3 = tex2D(_DispTex, IN.uv_DispTex - duv.zy).b;
        v4 = tex2D(_DispTex, IN.uv_DispTex + duv.zy).b;
        range = val;
    }
    o.Albedo = lerp(_ColorNeg.rgb, _Color.rgb, range);
    o.Normal = normalize(float3(v1 - v2, v3 - v4, 0.3));
}

ENDCG

}

FallBack "Diffuse"

}