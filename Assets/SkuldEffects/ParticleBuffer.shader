Shader "Skuld/Effects/GPU Particles/Buffer"
{
    Properties
    {
		[hdr]_Buffer ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
		cull Back

        Pass
        {
            CGPROGRAM
			#pragma target 5.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

			sampler2D _Buffer;
			float4 _Buffer_ST;
			float4 _Buffer_TexelSize;

            v2f vert (appdata v)
            {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				if (any(_ScreenParams.xy != abs(_Buffer_TexelSize.zw)))
				{
					o.vertex = 0;
				}
				o.uv = v.uv;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
				float4 col = tex2D(_Buffer, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
