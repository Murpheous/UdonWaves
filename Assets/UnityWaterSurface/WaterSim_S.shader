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
        _Lambda2Pi("Lambda2Pi",Range(6,20)) = 10
        _Effect("Effect",Vector) = (0,0,0,0)
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
    float _Lambda2Pi;
    float4 _Effect;
    float _CFAbsorb;
    sampler2D _ObstacleTex2D;
    

float laplacian(float2 pos, float3 duv)
{
    //float stateXp1 = A(pos + duv.xz).x;
    //float stateXm1 = tex2D(_SelfTexture2D, pos - duv.xz).x;
    //float stateYp1 = tex2D(_SelfTexture2D, pos + duv.zy).x;
    //float stateYm1 = tex2D(_SelfTexture2D, pos - duv.zy).x;
    return (A(pos + duv.xz).x + A(pos - duv.xz).x + A(pos + duv.zy).x + A(pos - duv.zy).x - 4.0 * A(pos).x);
    //return (stateXp1 + stateXm1 + stateYp1 + stateYm1 - 4.0 * A(pos).x);
}

float2 newField(float2 pos, float3 duv)
{

    float2 field = A(pos).xy;
    float currentTime = A(pos).a;
    float cycletime = currentTime * _T2Radians; // _Time = (t/20, t, t*2, t*3
    int2 effectpos = int2(floor(_Effect.x),floor(_CustomRenderTextureHeight*pos.y));
    int2 pixelPos = int2(floor(pos.x * _CustomRenderTextureWidth), floor(pos.y * _CustomRenderTextureHeight));
    //float force = 0.1* exp(-0.1 * dot(pixelPos - effectpos, pixelPos - effectpos)) * sin(cycletime);
    float force =  lerp(0, 0.5 * sin(cycletime),(int)(effectpos == pixelPos));

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
    float vScaled = _Lambda2Pi * updated.y;
    return float4(updated.x,updated.y,vScaled,A(pos).a+_DeltaT);
}
/*
float4 frag(v2f_customrendertexture i) : SV_Target
{
    float2 pos = i.globalTexcoord;
    float du = 1.0 / _CustomRenderTextureWidth;
    float dv = 1.0 / _CustomRenderTextureHeight;
    float3 duv = float3(du, dv, 0);
    float4 stateData = tex2D(_SelfTexture2D, pos);

    float state = stateData.r;
    float stateM1 = stateData.g;

    int xPixel = (int)(floor(pos.x * _CustomRenderTextureWidth));
    int yPixel = (int)(floor(pos.y * _CustomRenderTextureHeight));
    // 
    int isOnObstacle = (int)(tex2D(_ObstacleTex2D, pos).b > 0.5);
    int atObstacleMaxX = (int)(!isOnObstacle && (tex2D(_ObstacleTex2D, pos + duv.xz).b > 0.5));
    int atObstacleMinY = (int)(!isOnObstacle && (tex2D(_ObstacleTex2D, pos - duv.xz).b > 0.5));

    float stateXp1 = tex2D(_SelfTexture2D, pos + duv.xz).x;
    float stateXm1 = tex2D(_SelfTexture2D, pos - duv.xz).x;
    float stateYp1 = tex2D(_SelfTexture2D, pos + duv.zy).x;
    float stateYm1 = tex2D(_SelfTexture2D, pos - duv.zy).x;

    // Absorbing Stuff
    int atMinX = (int)((xPixel < 1) || (atObstacleMinY > 0));
    int atMaxX = (int)((xPixel >= (_CustomRenderTextureWidth - 1)) || (atObstacleMaxX > 0));
    int atMinY = (int)(yPixel <= 1);
    int atMaxY = (int)(yPixel >= (_CustomRenderTextureHeight - 1));
    int onBoundary = (int)((atMinX + atMaxX + atMaxY + atMinY) > 0);

    float b = 0;
    //
    float absL = tex2D(_SelfTexture2D, pos + duv.xz).g + _CFAbsorb * (stateXp1 - stateM1);
    b = lerp(b, absL, atMinX);
    float absR = tex2D(_SelfTexture2D, pos - duv.xz).g + _CFAbsorb * (stateXm1 - stateM1);
    b = lerp(b, absR, atMaxX);
    float absB = tex2D(_SelfTexture2D, pos + duv.zy).g + _CFAbsorb * (stateYp1 - stateM1);
    b = lerp(b, absB, atMinY);
    float absT = tex2D(_SelfTexture2D, pos - duv.zy).g + _CFAbsorb * (stateYm1 - stateM1);
    b = lerp(b, absT, atMaxY);
    
    state = lerp(state, b, onBoundary);

    // Mask off blue areas
    state = lerp(state, 0, isOnObstacle);

    float attenFactor = lerp(1.0, 0.995, _Attenuation);
    // Calculate update
    float laplacianState = stateXm1 + stateXp1 + stateYp1 + stateYm1;
    float wavePlus1 = lerp(((2 * state - stateM1 + _CdTdXsq * (laplacianState - 4 * state))* attenFactor), state, onBoundary);
 
    // Inject Effect
    float effectDelta = clamp(abs(xPixel - floor(_Effect.x)), 0, 1);
    float currentTime = A(pos).a;
    float cycletime = currentTime * _T2Radians; // _Time = (t/20, t, t*2, t*3
    float force = sin(cycletime);
    wavePlus1 = lerp(force, wavePlus1, effectDelta);
    // Accumulate currentEnergy
    float currentEnergy = wavePlus1 * wavePlus1;
    // at effect reset move previousMax to savedEnergy;   
    //int reset = (int)(_Effect.w > 0.9);
    //savedEnergy = lerp(savedEnergy, previousMax, reset);
    //previousMax = lerp(previousMax, 0.0, reset);
    // check if currentEnergy > previousMax (Energy)
    //previousMax += (currentEnergy / 20);
    return float4(wavePlus1, state, wavePlus1, A(pos).a + _DeltaT);
} */
/*
float4 fragAbsorb(v2f_customrendertexture i) : SV_Target
{
    float2 uv = i.globalTexcoord;
    float du = 1.0 / _CustomRenderTextureWidth;
    float dv = 1.0 / _CustomRenderTextureHeight;
    float3 duv = float3(du, dv, 0);
    float4 stateData = tex2D(_SelfTexture2D, uv);

    float state = stateData.r;
    float stateM1 = stateData.g;
    int xPixel = (int)(floor(uv.x * _CustomRenderTextureWidth));
    int yPixel = (int)(floor(uv.y * _CustomRenderTextureHeight));

    int isOnObstacle = (int)(tex2D(_ObstacleTex2D, uv).b > 0.5);
    int atObstacleMaxX = (int)(!isOnObstacle && (tex2D(_ObstacleTex2D, uv + duv.xz).b > 0.5));
    int atObstacleMinY = (int)(!isOnObstacle && (tex2D(_ObstacleTex2D, uv + duv.xz).b > 0.5));

    float stateXp1 = tex2D(_SelfTexture2D, uv + duv.xz).r;
    float stateXm1 = tex2D(_SelfTexture2D, uv - duv.xz).r;    
    float stateYp1 = tex2D(_SelfTexture2D, uv + duv.zy).r;
    float stateYm1 = tex2D(_SelfTexture2D, uv - duv.zy).r;

    int atMinX = (int)(xPixel < 1);
    int atMaxX = (int)((xPixel >= (_CustomRenderTextureWidth - 1)) || (atObstacleMaxX > 0));
    int atMinY = (int)(yPixel <= 1);
    int atMaxY = (int)(yPixel >= (_CustomRenderTextureHeight - 1));
    int onBoundary = (int)((atMinX + atMaxX + atMaxY + atMinY) > 0);


    float b = 0;
//
    float absL = tex2D(_SelfTexture2D, uv + duv.xz).g + _CFAbsorb * (stateXp1 - stateM1);
    b = lerp(b, absL, atMinX);
    float absR = tex2D(_SelfTexture2D, uv - duv.xz).g + _CFAbsorb * (stateXm1 - stateM1);
    b = lerp(b, absR, atMaxX);
    float absB = tex2D(_SelfTexture2D, uv + duv.zy).g + _CFAbsorb * (stateYp1 - stateM1);
    b = lerp(b, absB, atMinY);
    float absT = tex2D(_SelfTexture2D, uv - duv.zy).g + _CFAbsorb * (stateYm1 - stateM1);
    b = lerp(b, absT, atMaxY);

    // Update the boundary absorption

    state = lerp(state, b, onBoundary);

    // Mask off blue areas
    state = lerp(state, 0, isOnObstacle);

    stateData.x = state;
    return stateData;
}
*/

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
    /*
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
        Name "Absorb"
        CGPROGRAM
        #pragma vertex CustomRenderTextureVertexShader
        #pragma fragment fragAbsorb
        ENDCG
    }
    */
}

}