Shader "Water/Simulation"
{

    Properties
    {
        _ViewSelection("Show A=0, A^2=1, E=2",Range(0.0,2.0)) = 0.0
        _wallAttenDistance("Atten pixels from wall",Range(1,250)) = 20
        _CFLSq("CFL^2", Range(0.0, 0.5)) = 0.2
        _Effect("Effect",Vector) = (0,0,0,0)
        _CFAbsorb("CFAbsorb", Range(0.0,1.0)) = 0.2
        _ObstacleTex2D("Obstacle Image", 2D) = "black" {}
    }

    CGINCLUDE

    #include "UnityCustomRenderTexture.cginc"

    float _ViewSelection;
    float _wallAttenDistance;
    float _CFLSq;
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
    int IsSquared = (int)((_ViewSelection > 0.5) && (_ViewSelection < 1.5));
    float du = 1.0 / _CustomRenderTextureWidth;
    float dv = 1.0 / _CustomRenderTextureHeight;
    float3 duv = float3(du, dv, 0);
    float4 stateData = tex2D(_SelfTexture2D, uv);
    uint4 stateUInt = asuint(stateData);
    int xPixel = (int)(floor(uv.x * _CustomRenderTextureWidth));
    int yPixel = (int)(floor(uv.y * _CustomRenderTextureHeight));
    
    float highDelta = _CustomRenderTextureWidth - xPixel;
    float xDelta = highDelta; //lerp(xPixel, highDelta, (int)(xPixel <= highDelta));
    highDelta = _CustomRenderTextureHeight - yPixel;
    float yDelta = lerp(yPixel, highDelta, (int)(yPixel >= highDelta));
    float wallDelta = lerp(yDelta, xDelta, (int)(yDelta >= xDelta));
    //wallDelta = lerp(delta, wallDelta, (int)(wallDelta <= delta));
    float delta = clamp(abs(wallDelta / _wallAttenDistance),0,1);
    float attenFactor = lerp(0.996, 1.0, delta);
    // Calculate update
    float wn = stateData.r;
    float wnm1 = stateData.g;
    float wnmzy = tex2D(_SelfTexture2D, uv - duv.zy).r;
    float wnpzy = tex2D(_SelfTexture2D, uv + duv.zy).r;
    float wnmxz = tex2D(_SelfTexture2D, uv - duv.xz).r;
    float wnpxz = tex2D(_SelfTexture2D, uv + duv.xz).r;

    // Calculate the wave update
    float wnp1 = ((2 * wn - wnm1 + _CFLSq * (wnmzy + wnpzy + wnmxz + wnpxz - 4 * wn))) *attenFactor;
  
    // Inject Effect
    float effectDelta = clamp(abs(xPixel - floor(_Effect.x)), 0, 1);
    wnp1 = lerp(_Effect.z, wnp1, effectDelta);

    // Add Effect into the mix
    float output = lerp(wnp1, wnp1 * wnp1, IsSquared);
    return float4(wnp1, wn, output, IsSquared);
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