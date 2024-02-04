Shader "Water/Simulation"
{

    Properties
    {
        // _ViewSelection("Show A=0, A^2=1, E=2",Range(0.0,2.0)) = 0.0
        _CdTdXsq("CdTdX^2", Float) = 0.2
        _CdTdX("CdTdX", Float) = 0.25
        _K("K",Float) = 0.25
        _KSquared("K Squared",Float) = 0.0625
        _DeltaT("DeltaT",Float) = 1
        _T2Radians("T2Radians",Float) = 6.283
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
    float _CdTdXsq;
    float _CdTdX; // c/2pi
    float _K;
    float _KSquared;
    float _DeltaT;
    float _T2Radians;
    float4 _DriveSettings;
    float _DriveAmplitude;
    float _CFAbsorb;
    sampler2D _ObstacleTex2D;
    

float laplacian(float2 pos, float3 duv)
{
    return (A(pos + duv.xz).x + A(pos - duv.xz).x + A(pos + duv.zy).x + A(pos - duv.zy).x - 4.0 * A(pos).x);
}

float3 newField(float2 pos, float3 duv)
{
    float3 field = A(pos).xyz;
    int2 pixelPos = int2(floor(pos.x * _CustomRenderTextureWidth), floor(pos.y * _CustomRenderTextureHeight));
    float df = laplacian(pos,duv); // force from curvature
    field.y += _CdTdX * df; //velocity += force * time step
    field.x += _CdTdX * field.y; //position += velocity*time step
    field.z = field.y / _K; 
    return field;
}

float2 newAbsorbed(float2 pos, float2 n,float3 duv)
{
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
    float3 updated = float3(0, 0, 0);
    
    // Pixel Positions
    int xPixel = (int)(floor(pos.x * _CustomRenderTextureWidth));
    int yPixel = (int)(floor(pos.y * _CustomRenderTextureHeight));
    int maxX = _CustomRenderTextureWidth - 2;
    int maxY = _CustomRenderTextureHeight - 2;
    // Barrier & Boundaries
    float3 Obs = O(pos).rgb;
    bool isOnObstacle = (Obs.b > 0.5);
    bool isUser = !isOnObstacle && ((Obs.r + Obs.g) > 0.1);
    bool atObstacleMaxX = (!isOnObstacle && (O(pos + duv.xz).b > 0.5));
    bool atObstacleMinX = (!isOnObstacle && (O(pos - duv.xz).b > 0.5));
    bool atObstacleMaxY = (!isOnObstacle && (O(pos + duv.zy).b > 0.5));
    bool atObstacleMinY = (!isOnObstacle && (O(pos - duv.zy).b > 0.5));
    bool  inDriveRow = ((int)floor(_DriveSettings.x) == xPixel);
    bool  inDriveColumns = inDriveRow && ((floor(_DriveSettings.y) <= yPixel) && (floor(_DriveSettings.z) >= yPixel));


    if (inDriveColumns)
    {
        float tRadians = A(pos).a * _T2Radians;
        updated.x = _DriveAmplitude * sin(tRadians);
    }
    else if (isUser)
        updated.x=0;
    else if (!isOnObstacle)
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
  
    return float4(updated.x, updated.y, updated.z,A(pos).a+_DeltaT);
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