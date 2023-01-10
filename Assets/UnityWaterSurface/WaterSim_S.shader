Shader "Water/Simulation"
{

    Properties
    {
        // _ViewSelection("Show A=0, A^2=1, E=2",Range(0.0,2.0)) = 0.0
        _Attenuation("Attenuation",Range(0,1)) = 0
        _CdTdXaq("Cdtdx^2", Range(0.0, 0.5)) = 0.2
        _Cbar("Cbar", Float) = 1
        _C("C", Float) = 1
        _DeltaT("DeltaT",Float) = 1
        _Effect("Effect",Vector) = (0,0,0,0)
        _CFAbsorb("CFAbsorb", Range(0.0,1.0)) = 0.2
        _ObstacleTex2D("Obstacle Image", 2D) = "black" {}
    }

    CGINCLUDE

    #include "UnityCustomRenderTexture.cginc"

    //float _ViewSelection;
    float _Attenuation;
    float _CdTdXaq;
    float _Cbar; // c/2pi
    float _C; // c/2pi
    float _DeltaT;
    float4 _Effect;
    float _CFAbsorb;
    sampler2D _ObstacleTex2D;
    
    struct StateInfo
    {
        float wStatePrev;
        float wState;
        float wStateNext;
    };

float4 frag(v2f_customrendertexture i) : SV_Target
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
    // 
    int isBlue = (int)(tex2D(_ObstacleTex2D, uv).b > 0.5);
    int blueRight = (int)((isBlue == 0) && (tex2D(_ObstacleTex2D, uv + duv.xz).b > 0.5));
    int blueLeft = (int)((isBlue == 0) && (tex2D(_ObstacleTex2D, uv + duv.xz).b > 0.5));

    float stateXp1 = tex2D(_SelfTexture2D, uv + duv.xz).r;
    float stateXm1 = tex2D(_SelfTexture2D, uv - duv.xz).r;
    float stateYp1 = tex2D(_SelfTexture2D, uv + duv.zy).r;
    float stateYm1 = tex2D(_SelfTexture2D, uv - duv.zy).r;

    /* Absorbing Stuff*/
    int atLeft = (int)(xPixel < 1);
    int atRight = (int)((xPixel >= (_CustomRenderTextureWidth - 1)) || (blueRight > 0));
    int atBottom = (int)(yPixel <= 1);
    int atTop = (int)(yPixel >= (_CustomRenderTextureHeight - 1));
    int onBoundary = (int)((atLeft + atRight + atTop + atBottom) > 0);

    float b = 0;
    //
    float absL = tex2D(_SelfTexture2D, uv + duv.xz).g + _CFAbsorb * (stateXp1 - stateM1);
    b = lerp(b, absL, atLeft);
    float absR = tex2D(_SelfTexture2D, uv - duv.xz).g + _CFAbsorb * (stateXm1 - stateM1);
    b = lerp(b, absR, atRight);
    float absB = tex2D(_SelfTexture2D, uv + duv.zy).g + _CFAbsorb * (stateYp1 - stateM1);
    b = lerp(b, absB, atBottom);
    float absT = tex2D(_SelfTexture2D, uv - duv.zy).g + _CFAbsorb * (stateYm1 - stateM1);
    b = lerp(b, absT, atTop);
    
    state = lerp(state, b, onBoundary);

    // Mask off blue areas
    state = lerp(state, 0, isBlue);

    float attenFactor = lerp(1.0, 0.995, _Attenuation);
    // Calculate update
    float fourNeighbours = stateXm1 + stateXp1 + stateYp1 + stateYm1;
    float wavePlus1 = lerp(((2 * state - stateM1 + _CdTdXaq * (fourNeighbours - 4 * state))* attenFactor), state, onBoundary);
 
    // Inject Effect
    float effectDelta = clamp(abs(xPixel - floor(_Effect.x)), 0, 1);
    wavePlus1 = lerp(_Effect.z, wavePlus1, effectDelta);
    // Accumulate currentEnergy
    float currentEnergy = wavePlus1 * wavePlus1;
    // at effect reset move previousMax to savedEnergy;   
    float previousMax = tex2D(_SelfTexture2D, uv).a;
    float savedEnergy = tex2D(_SelfTexture2D, uv).b;
    int reset = (int)(_Effect.w > 0.9);
    savedEnergy = lerp(savedEnergy, previousMax, reset);
    previousMax = lerp(previousMax, 0.0, reset);
    // check if currentEnergy > previousMax (Energy)
    previousMax += (currentEnergy / 20);
    return float4(wavePlus1, state, savedEnergy,previousMax);
}

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

    int isBlue = (int)(tex2D(_ObstacleTex2D, uv).b > 0.5);
    int blueRight = (int)((isBlue == 0) && (tex2D(_ObstacleTex2D, uv + duv.xz).b > 0.5));
    int blueLeft = (int)((isBlue == 0) && (tex2D(_ObstacleTex2D, uv + duv.xz).b > 0.5));

    float stateXp1 = tex2D(_SelfTexture2D, uv + duv.xz).r;
    float stateXm1 = tex2D(_SelfTexture2D, uv - duv.xz).r;    
    float stateYp1 = tex2D(_SelfTexture2D, uv + duv.zy).r;
    float stateYm1 = tex2D(_SelfTexture2D, uv - duv.zy).r;

    int atLeft = (int)(xPixel < 1);
    int atRight = (int)((xPixel >= (_CustomRenderTextureWidth - 1)) || (blueRight > 0));
    int atBottom = (int)(yPixel <= 1);
    int atTop = (int)(yPixel >= (_CustomRenderTextureHeight - 1));
    int onBoundary = (int)((atLeft + atRight + atTop + atBottom) > 0);


    float b = 0;
//
    float absL = tex2D(_SelfTexture2D, uv + duv.xz).g + _CFAbsorb * (stateXp1 - stateM1);
    b = lerp(b, absL, atLeft);
    float absR = tex2D(_SelfTexture2D, uv - duv.xz).g + _CFAbsorb * (stateXm1 - stateM1);
    b = lerp(b, absR, atRight);
    float absB = tex2D(_SelfTexture2D, uv + duv.zy).g + _CFAbsorb * (stateYp1 - stateM1);
    b = lerp(b, absB, atBottom);
    float absT = tex2D(_SelfTexture2D, uv - duv.zy).g + _CFAbsorb * (stateYm1 - stateM1);
    b = lerp(b, absT, atTop);

    // Update the boundary absorption

    state = lerp(state, b, onBoundary);

    // Mask off blue areas
    state = lerp(state, 0, isBlue);

    stateData.x = state;
    return stateData;
}

float4 frag_left_click(v2f_customrendertexture i) : SV_Target
{
    float2 uv = i.globalTexcoord;
    float4 stateData = tex2D(_SelfTexture2D, uv);
    stateData.x = -1;
    return stateData;
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
        Name "Absorb"
        CGPROGRAM
        #pragma vertex CustomRenderTextureVertexShader
        #pragma fragment fragAbsorb
        ENDCG
    }
}

}