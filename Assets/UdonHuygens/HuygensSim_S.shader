Shader"Simulation/Huygens"
{

    Properties
    {
        // _ViewSelection("Show A=0, A^2=1, E=2",Range(0.0,2.0)) = 0.0
        _LambdaPx("Lambda Pixels", float) = 49.64285714
        _LeftPx("Left Edge",float) = 50
        _RightPx("Right Edge",float) = 1964
        _UpperEdge("Upper Edge",float) = 972
        _LowerEdge("Lower Edge",float) = 76
        _NumSources("Num Sources",float) = 2
        _SlitPitchPx("Slit Pitch",float) = 448
        _SlitWidePx("Slit Width", Range(1.0,40.0)) = 12.0
        _Color("Colour Wave", color) = (1, 1, 0, 0)
        _ColorNeg("Colour Base", color) = (0, 0.3, 1, 0)
        _ColorVel("Colour Velocity", color) = (0, 0.3, 1, 0)
        _ColorFlow("Colour Flow", color) = (1, 0.3, 0, 0)
        _DisplayMode("Display Mode", float) = 0
        _SourcePhase("Source Phase", float) = 0
        _Scale("Simulation Scale",Range(1.0,10.0)) = 1
    }

CGINCLUDE

#include "UnityCustomRenderTexture.cginc"
    
//#define A(U)  tex2D(_SelfTexture2D, float2(U))

float _LambdaPx;
float _LeftPx;
float _RightPx;
float _UpperEdge;
float _LowerEdge;
int _NumSources;
float _SlitPitchPx;
float _SlitWidePx;
float4 _Color;
float4 _ColorNeg;
float4 _ColorVel;
float4 _ColorFlow;
float _DisplayMode;
float _SourcePhase;
float _Scale;
static const float Tau = 6.28318531f;
static const float PI = 3.14159265f;
   
float2 sourcePhasor(float2 delta)
{
    float rPixels = length(delta);
    float rLambda = rPixels/_LambdaPx;
    float rPhi =  rLambda*Tau + _SourcePhase;
    float amp = _Scale*_LambdaPx/max(_LambdaPx,rPixels);
    float2 result = float2(cos(rPhi),sin(rPhi));
    return result * amp;
}

float4 frag(v2f_customrendertexture i) : SV_Target
{
    float2 pos = i.globalTexcoord;
    float du = 1.0 / _CustomRenderTextureWidth;
    float dv = 1.0 / _CustomRenderTextureHeight;
    float3 duv = float3(du, dv, 0);
    float4 updated = float4(1, 0, 1,1);
    
    // Pixel Positions
    int xPixel = (int)(floor(pos.x * _CustomRenderTextureWidth));
    int yPixel = (int)(floor(pos.y * _CustomRenderTextureHeight));
    bool isInMargin = (xPixel >= _LeftPx) && (xPixel <= _RightPx);
    bool isInHeadFoot = (yPixel >= _LowerEdge) && (yPixel <= _UpperEdge);
    float2 phasor = float2(0,0);
    int slitWidthCount = (int) (max(1.0, _SlitWidePx));
    int sourceCount = round(_NumSources);
    float pixScale = 1 / _Scale;
    float sourceY = ((_NumSources - 1) * +_SlitPitchPx) * 0.5 + (_SlitWidePx * 0.25);
    float2 delta = float2(abs(xPixel-_LeftPx)*_Scale,0.0);
    float yScaled = (yPixel - _CustomRenderTextureHeight / 2.0)*_Scale;
    for (int nAperture = 0; nAperture < sourceCount; nAperture++)
    {
        float slitY = sourceY;
        float2 phaseAmp = float2(0, 0);
        for (int pxCount = 0; pxCount < slitWidthCount; pxCount++)
        {
             delta.y = abs(yScaled-slitY);
             phaseAmp += sourcePhasor(delta);
             slitY -= 1;
        }
        phasor += phaseAmp;
        sourceY -= _SlitPitchPx;
    }
    float alpha = 0;
    if (isInMargin && isInHeadFoot)
    {
        if (_DisplayMode < 2)
        {
            alpha = phasor.x;
            if (_DisplayMode > 0.1)
            {
                alpha *= alpha;
                updated = lerp(_ColorNeg, _Color, alpha);
            }
            else
            {
                updated = lerp(_ColorNeg, _Color, alpha);
                alpha = (alpha + 1);
            }
            updated.a = clamp(alpha, 0, 1); //      alpha;
        }
        else if (_DisplayMode < 3.9)
        {
            alpha = phasor.y;
            if (_DisplayMode > 2.1)
            {
                alpha *= alpha;
                updated = lerp(_ColorNeg, _ColorVel, alpha);
            }
            else
            {
                updated = lerp(_ColorNeg, _ColorVel, alpha);
                alpha = (alpha + 1);
            }
            updated.a = clamp(alpha,0,1);
        }
        else
        {
            alpha = (phasor.x * phasor.x) + (phasor.y * phasor.y);
            updated = lerp(_ColorNeg, _ColorFlow, alpha);
            updated.a = clamp(alpha, 0, 1);
        }
    }
    else
    {
        updated = _ColorNeg;
        updated.a = 0.33;
    }
    return updated;
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
    }
}