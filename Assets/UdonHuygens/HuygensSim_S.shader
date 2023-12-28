Shader "Simulation/Huygens"
{

    Properties
    {
        // _ViewSelection("Show A=0, A^2=1, E=2",Range(0.0,2.0)) = 0.0
        _LambdaPx("LambdaPixels", float) = 49.77777778
        _LeftPx("LeftEdge",float) = 50
        _RightPx("RightEdge",float) = 1964
        _TopPx("TopEdge",float) = 60
        _BottomPx("LowerEdge",float) = 1964
        _NumApertures("ApertureCount",int) = 2
        _AperturePitch("AperturePitch",float) = 448
        _Color("Colour Wave", color) = (1, 1, 0, 0)
        _ColorNeg("Colour Base", color) = (0, 0.3, 1, 0)
        _ColorVel("Colour Velocity", color) = (0, 0.3, 1, 0)
        _ColorFlow("Colour Flow", color) = (1, 0.3, 0, 0)
        _DisplayMode("Display Mode", float) = 0
        _SourcePhase("Source Phase", float) = 0
    }

    CGINCLUDE

    #include "UnityCustomRenderTexture.cginc"
    
    //#define A(U)  tex2D(_SelfTexture2D, float2(U))

    float _LambdaPx;
    float _LeftPx;
    float _RightPx;
    float _TopPx;
    float _BottomPx;
    int _NumApertures;
    float _AperturePitch;
    float4 _Color;
    float4 _ColorNeg;
    float4 _ColorVel;
    float4 _ColorFlow;
    float _DisplayMode;
    float _SourcePhase;
    static const float Tau = 6.28318531f;
    static const float PI = 3.14159265f;
   
    float2 sourcePhasor(float2 delta)
    {
        float rLambda = length(delta)/_LambdaPx;
        float rPhi =  rLambda*Tau + _SourcePhase;
        float amp = 5/max(1.0,rLambda);
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
        bool isInMargin = xPixel >= _LeftPx && xPixel <= _RightPx;
        bool isInHeadFoot = (yPixel >= _TopPx) && (yPixel <= _BottomPx);
        float2 phasor = float2(0,0);
        float sourceY = (_CustomRenderTextureHeight + ((_NumApertures-1)*_AperturePitch))/2.0;
        float2 delta = float2(abs(xPixel-_LeftPx),0.0);
        for (int nAperture = 0; nAperture< _NumApertures; nAperture++)
        {
            delta.y = abs(sourceY-yPixel);
            float2 phaseAmp = sourcePhasor(delta);
            phasor += (phaseAmp);//*phaseAmp.a;
            sourceY -= _AperturePitch;
        }
        phasor = phasor/(_NumApertures);
        float alpha = 0;
        if (isInMargin && isInHeadFoot)
        {
            if (_DisplayMode < 2)
            {
                alpha = phasor.x;
                if (_DisplayMode > 0.1)
                    alpha *= alpha;
                updated = _Color;
                updated.a =alpha;
            }
            else if (_DisplayMode < 3.9)
            {
                alpha = phasor.y;
                if (_DisplayMode > 2.1)
                    alpha *= alpha;
                updated = _ColorVel;
                updated.a =alpha;
            }
            else
            {
                alpha = (phasor.x * phasor.x) + (phasor.y * phasor.y);
                updated = _ColorFlow;
                updated.a =alpha;
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