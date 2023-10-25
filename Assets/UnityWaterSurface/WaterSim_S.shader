Shader "Water/Simulation"
{

    Properties
    {
        // _ViewSelection("Show A=0, A^2=1, E=2",Range(0.0,2.0)) = 0.0
        _Attenuation("Attenuation",Range(0,1)) = 0
        _CdTdXsq("CdTdX^2", Float) = 0.2
        _CdTdX("CdTdX", Float) = 0.25
        _DeltaT("DeltaT",Float) = 1
        _T2Radians("T2Radians",Float) = 6.283
        _Lambda2PI("Lambda2PI",Float) = 10
        _DriveSettings("DriveSettings",Vector) = (0,0,0,0)
        _DriveAmplitude("DriveAmplitude",Float) = 0
        _CFAbsorb("CFAbsorb", Range(0.0,1.0)) = 0.2
        _ObstacleTex2D("Obstacle Image", 2D) = "black" {}
    }

    CGINCLUDE

    #include "UnityCustomRenderTexture.cginc"
    
    #define A(U)  tex2D(_SelfTexture2D, float2(U))
    #define O(U)  tex2D(_ObstacleTex2D, float2(U))

        //float _ViewSelection;
    float _Attenuation;
    float _CdTdXsq;
    float _CdTdX; // c/2pi
    float _DeltaT;
    float _T2Radians;
    float _Lambda2PI;
    float4 _DriveSettings;
    float _DriveAmplitude;
    float _CFAbsorb;
    sampler2D _ObstacleTex2D;
    

float laplacian(float2 pos, float3 duv)
{
    return (A(pos + duv.xz).x + A(pos - duv.xz).x + A(pos + duv.zy).x + A(pos - duv.zy).x - 4.0 * A(pos).x);
}

float2 newField(float2 pos, float3 duv)
{
    float2 field = A(pos).xy;
    float currentTime = A(pos).a;
    float cycletime = currentTime * _T2Radians; // _Time = (t/20, t, t*2, t*3
    int2 pixelPos = int2(floor(pos.x * _CustomRenderTextureWidth), floor(pos.y * _CustomRenderTextureHeight));
    // DriveSettings x= distance down tank, y & z region across tank;
    int  inDriveRow = (int)((int)floor(_DriveSettings.x) == pixelPos.x);
    int  inDriveColumns = (int)((floor(_DriveSettings.y) <= pixelPos.y) && (floor(_DriveSettings.z) >= pixelPos.y));
    float force =  lerp(0, _DriveAmplitude * sin(cycletime),(int)(inDriveRow==1 && inDriveColumns==1));
    field.y += _CdTdX * (laplacian(pos,duv) + force); //velocity += force * time step
    field.x += _CdTdX * field.y; //position += velocity*time step
    return field;
}

/*
vec2 newAbsorbed(vec2 pos, vec2 n)
{
    float uS = 1.0 * dt / 1.0;
    vec2 field = A(pos).xy;
    field.x = A(pos + n).x + (newField(pos + n).x - A(pos).x) * (uS - 1.0) / (uS + 1.0);
    return field;
}

*/

float2 newAbsorbed(float2 pos, float2 n,float3 duv)
{
    //float uS = 1.0 * _DeltaT / 1.0;
    float2 field = A(pos).xy;
    float2 offset = pos + n;
    field.x = A(offset).x + (newField(offset,duv).x - A(pos).x) * (_CFAbsorb);
    return field;
}

float4 fragNew(v2f_customrendertexture i) : SV_Target
{
    float2 pos = i.globalTexcoord;
    float du = 1.0 / _CustomRenderTextureWidth;
    float dv = 1.0 / _CustomRenderTextureHeight;
    float3 duv = float3(du, dv, 0);
    float2 updated = float2(0, 0);
    
    // Pixel Positions
    int xPixel = (int)(floor(pos.x * _CustomRenderTextureWidth));
    int yPixel = (int)(floor(pos.y * _CustomRenderTextureHeight));
    int maxX = _CustomRenderTextureWidth - 2;
    int maxY = _CustomRenderTextureHeight - 2;
    // Barrier & Boundaries
    bool isOnObstacle = (O(pos).b > 0.5);
    bool atObstacleMaxX = (!isOnObstacle && (O(pos + duv.xz).b > 0.5));
    bool atObstacleMinX = (!isOnObstacle && (O(pos - duv.xz).b > 0.5));
    bool atObstacleMaxY = (!isOnObstacle && (O(pos + duv.zy).b > 0.5));
    bool atObstacleMinY = (!isOnObstacle && (O(pos - duv.zy).b > 0.5));

    if (!isOnObstacle)
    {
        if ((xPixel <= 1) || atObstacleMinX)
            updated.xy = newAbsorbed(pos, duv.xz, duv);
        else if ((xPixel >= maxX) || atObstacleMaxX)
            updated.xy = newAbsorbed(pos, -duv.xz, duv);
        else if ((yPixel <= 1) || atObstacleMinY)
            updated.xy = newAbsorbed(pos, duv.zy, duv);
        else if ((yPixel >= maxY) || atObstacleMaxY)
            updated.xy = newAbsorbed(pos, -duv.zy, duv);
        else //if (!onBoundary)
            updated = newField(pos, duv);
    }
  
    return float4(updated.x,updated.y, _Lambda2PI * updated.y,A(pos).a+_DeltaT);
}

ENDCG

SubShader
{
    Cull Off ZWrite Off ZTest Always
    Pass
    {
        Name "UpdateNew"
        CGPROGRAM
        #pragma vertex CustomRenderTextureVertexShader
        #pragma fragment fragNew
        ENDCG
    }

}

}