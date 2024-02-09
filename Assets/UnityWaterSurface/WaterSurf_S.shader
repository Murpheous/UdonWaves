Shader"Water/Surface"
{

Properties
{
    _WaveTex("Wave Texture", 2D) = "gray" {}
    _MeshSpacing("Mesh XY",Vector) = (0.003,0.003,0.00125,0.002)
    _VertexNormals("Use vertex normals",float) = 1
    _SurfNormals("Use surface normals",float) = 0
    _Color("Color", color) = (1, 1, 0, 0)
    _ColorFlat("Flat Color", color) = (0, 0.3, 1, 0)
    _ColorBase("Base Color", color) = (0, 0.1, .1, 0)
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
    Cull Off

    CGPROGRAM

    #pragma surface surf Standard alpha addshadow fullforwardshadows vertex:vert
    #pragma target 5.0

sampler2D _WaveTex;
float4 _WaveTex_TexelSize;
float4 _MeshSpacing;
float _VertexNormals;
float _SurfNormals;
fixed4 _Color;
fixed4 _ColorFlat;
fixed4 _ColorBase;
fixed4 _ColorVel;
fixed4 _ColorFlow;
float _UseHeight;
float _UseVelocity;
float _UseSquare;
float _K;
half _Glossiness;
half _Metallic;
float _Displacement;


struct Input 
{
    float2 uv_WaveTex;
};

float vSample(float2 coord )
{
    float hgt = 0;
    float vel = 0;
    float3 duv = float3(_WaveTex_TexelSize.xy, 0);

    if (_UseHeight > 0.5)
    {
        hgt = tex2Dlod(_WaveTex, float4(coord, 0, 0)).r;
        if (_UseSquare > 0.5)
            hgt *= hgt*.3;
    }
    if (_UseVelocity > 0.75)
    {
        vel = tex2Dlod(_WaveTex, float4(coord, 0, 0)).g / _K;
        if (_UseSquare > 0.5)
            vel *= vel*.3;
    }
    return (hgt + vel) * _Displacement;
}


float fSample(float2 coord, bool squared)
{
    float hgt = 0;
    float vel = 0;
    if (_UseHeight > 0.5)
    {
        hgt = tex2D(_WaveTex, coord).r;
        if (squared)
        {
            hgt *= hgt;
        }
    }
    if (_UseVelocity > 0.5)
    {
        vel = tex2D(_WaveTex, coord).g/ _K;
        if (squared)
        {
            vel *= vel;
        }
    }
    return hgt + vel;
}


float4 fQuadSample(float2 coord, float3 duv, bool squared)
{
    float r = fSample(coord - duv.xz, squared);
    float l = fSample(coord + duv.xz, squared);
    float u = fSample(coord - duv.zy, squared);
    float d = fSample(coord + duv.zy, squared);
    return float4 (r,l,u,d);
}

float4 vQuadSample(float2 coord)
{
    float3 duv = float3(_MeshSpacing.yz,0);
    float r = vSample(coord - duv.xz);
    float l = vSample(coord + duv.xz);
    float u = vSample(coord - duv.zy);
    float d = vSample(coord + duv.zy);
    return float4 (r,l,u,d);
}

void vert(inout appdata_full v)
{

    if (_VertexNormals > 0.5)
    {
        float h = vSample(v.texcoord.xy);
        float4 delta = vQuadSample(v.texcoord.xy);
        float d1 =  (delta.x - delta.y);
        float d2 = (delta.w - delta.z);
        v.normal = normalize(float3(d1,_MeshSpacing.x + _MeshSpacing.y,d2));
        v.vertex.y = (2*h + delta.x + delta.y + delta.z + delta.w)/6.0;
    }
}

void surf(Input IN, inout SurfaceOutputStandard o) 
{
    o.Metallic = _Metallic;
    o.Smoothness = _Glossiness;
    o.Alpha = 1;

    bool showSquared = (_UseSquare > 0.5);
    bool showAmp = (_UseHeight > 0.5);
    bool showVel = (_UseVelocity > 0.5);

    float3 duv = float3(_WaveTex_TexelSize.xy, 0);
    float value = fSample(IN.uv_WaveTex, showSquared);
    fixed4 theColor = _Color;
    if (showAmp && showVel)
       theColor = _ColorFlow;
    else if (showVel)
        theColor = _ColorVel;
    if (_SurfNormals > 0.5)
    {
        float4 delta = fQuadSample(IN.uv_WaveTex, duv, showSquared);
        float d1 = _Displacement * (delta.x - delta.y);
        float d2 = _Displacement * (delta.w - delta.z);
        o.Normal = normalize(float3(d1,2*_WaveTex_TexelSize.x,d2));
    }
    if (showSquared)
    {
        o.Albedo = lerp(_ColorFlat, theColor, value*1.25);
        o.Alpha = value + 0.3;
    }
    else
    {
        float abV = abs(value)*1.5;
        if (value >= 0)
        {
            o.Albedo = lerp(_ColorFlat, theColor, abV);
            o.Alpha = abV + 0.4;
        }
        else
        {
            o.Albedo = lerp(_ColorFlat, _ColorBase, abV);
            o.Alpha = clamp(0.4 + value,0,1);
        }
    }
    
}

ENDCG

}

FallBack "Diffuse"

}