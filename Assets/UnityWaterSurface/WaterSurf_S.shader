Shader"Water/Surface"
{

Properties
{
    _DispTex("Disp Texture", 2D) = "gray" {}
    _Color("Color", color) = (1, 1, 0, 0)
    _ColorNeg("ColorBase", color) = (0, 0.3, 1, 0)
    _ColorVel("ColorVelocity", color) = (0, 0.3, 1, 0)
    _ColorFlow("ColorFlow", color) = (1, 0.3, 0, 0)
    _UseHeight("Use Sfc Height",Range(0.0,1)) = 1    
    _UseVelocity("Use Sfc Velocity",Range(0.0,1)) = 0
    _UseSquare("Square Amplitude",Range(0.0,1)) = 1    
    _K("WaveNumber K",Range(0.001,1)) = 0.1
    _Glossiness("Smoothness", Range(0,1)) = 0.5
    _Metallic("Metallic", Range(0,1)) = 0.0
    _Displacement("Displacement", Range(0, 0.1)) = 0.01
}

SubShader
    {

    Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

    CGPROGRAM

    #pragma surface surf Standard alpha addshadow fullforwardshadows vertex:disp
    #pragma target 5.0

sampler2D _DispTex;
float4 _DispTex_TexelSize;
fixed4 _Color;
fixed4 _ColorNeg;
fixed4 _ColorVel;
fixed4 _ColorFlow;
float _UseHeight;
float _UseVelocity;
float _UseSquare;
float _K;
half _Glossiness;
half _Metallic;
float _Displacement;

/*
struct appdata 
{
    float4 vertex   : POSITION;
    float4 tangent  : TANGENT;
    float3 normal   : NORMAL;
    float2 texcoord : TEXCOORD0;
    float2 texcoord1 : TEXCOORD1;
    float2 texcoord2 : TEXCOORD2;
};
*/

struct Input 
{
    float2 uv_DispTex;
};
/*
float4 tessDistance(appdata v0, appdata v1, appdata v2) 
{
    return UnityDistanceBasedTess(v0.vertex, v1.vertex, v2.vertex, _MinDist, _MaxDist, _TessFactor);
}
*/
void disp(inout appdata_full v)
{
    float hgt = 0;
    float vel = 0;
    float3 p = v.vertex.xyz;
    if (_UseHeight > 0.5)
    {
        hgt = tex2Dlod(_DispTex, float4(v.texcoord.xy, 0, 0)).r;
        if (_UseSquare)
            hgt *= hgt;
    }
    if (_UseVelocity > 0.5)
    {
        vel = tex2Dlod(_DispTex, float4(v.texcoord.xy, 0, 0)).g / _K;
        if (_UseSquare)
            vel *= vel;
    }
    p.y = (hgt + vel) * _Displacement;
    v.vertex.xyz = p;
}

float sq(float x)
{
    return (x * x);
}

void surf(Input IN, inout SurfaceOutputStandard o) 
{
    o.Metallic = _Metallic;
    o.Smoothness = _Glossiness;
    o.Alpha = 1;

    bool showSquared = (_UseSquare > 0.5);
    bool showAmp = (_UseHeight > 0.5);
    bool showVel = (_UseVelocity > 0.5);

    float3 duv = float3(_DispTex_TexelSize.xy, 0);
    float value = 0;
    float4 delta = float4(0,0,0,0);
    fixed4 theColor = _Color;
    if (showAmp)
    {
        value = tex2D(_DispTex, IN.uv_DispTex).r;
        delta = float4( tex2D(_DispTex, IN.uv_DispTex - duv.xz).r,
                        tex2D(_DispTex, IN.uv_DispTex + duv.xz).r,
                        tex2D(_DispTex, IN.uv_DispTex - duv.zy).r, 
                        tex2D(_DispTex, IN.uv_DispTex + duv.zy).r);
        if (showSquared)
        {
            value *= value;
            delta *= delta;
        }
    }
    if (showVel)
    {
        float valueV = tex2D(_DispTex, IN.uv_DispTex).g/ _K;
        float4 deltaV = float4(tex2D(_DispTex, IN.uv_DispTex - duv.xz).g / _K,
                        tex2D(_DispTex, IN.uv_DispTex + duv.xz).g / _K,
                        tex2D(_DispTex, IN.uv_DispTex - duv.zy).g / _K,
                        tex2D(_DispTex, IN.uv_DispTex + duv.zy).g / _K);
        if (showSquared)
        {
            valueV *= valueV;
            deltaV *= deltaV;
        }
        theColor = _ColorVel;
        value += valueV;
        delta += deltaV;
    }
    if (showAmp && showVel)
    {
       theColor = _ColorFlow;
    }
    float range = showSquared ? value : (value+1);
        
    float d1 = _Displacement * (delta.y - delta.x);
    float d2 = _Displacement * (delta.w - delta.z);
    o.Albedo = lerp(_ColorNeg, theColor, range);
    o.Normal = normalize(float3(d1,2*_DispTex_TexelSize.x,d2));
}

ENDCG

}

FallBack "Diffuse"

}