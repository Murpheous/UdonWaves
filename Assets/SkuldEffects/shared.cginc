Shader "Skuld/Effects/GPU Particles/Render"
{
	Properties
	{
		_MainTex("Color Texture", 2D) = "white" {}
		[hdr]_Buffer("Compute Input Texture", 2D) = "white" {}
		_Vertices("Number of Vertices in Default Shape", int) = 0
		_Size("Particle Size", float) = 1
		[Toggle] _ZWrite("Z-Write",Float) = 1
	}
		SubShader
		{
			Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
			cull Off
			Blend SrcAlpha One
			Lighting Off
			SeparateSpecular Off
			ZWrite[_ZWrite]

			Pass
			{
				CGPROGRAM
				#pragma target 5.0
				#pragma vertex vert
				#pragma fragment frag
				#pragma geometry geom
				#pragma multi_compile_instancing
				#pragma multi_compile

				#include "shared.cginc"
				#include "UnityCG.cginc"

				struct appdata
				{
					float4 position : POSITION;
					float2 uv : TEXCOORD0;
					uint id : SV_VertexID;
				};

				struct v2f
				{
					float4 position : SV_POSITION;
					float3 wposition : W_POS;
					float2 uv : TEXCOORD0;
					uint id : VERTEXID;
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;
				sampler2D _Buffer;
				float4 _Buffer_ST;
				float4 _Buffer_TexelSize;
				float _Size;
				uint _Vertices;


				v2f vert(appdata v)
				{
					v2f o;
					o.position = v.position;
					o.wposition = float3(0, 0, 0);
					o.uv = v.uv;
					o.id = v.id;
					return o;
				}

				float4 getPosition(uint index) {
					float2 uv = IndexToUV(index, _Buffer_TexelSize);
					float4 output = tex2Dlod(_Buffer, float4(uv,0,0));
					return output;
				}

				[instance(20)]
				[maxvertexcount(12)]
				void geom(triangle v2f input[3], inout PointStream<v2f> pointStream, uint instanceID : SV_GSInstanceID) {
					uint index = 0;
					uint offset = (uint)_Vertices * instanceID;
					float4 position = float4(0, 0, 0, 0);

					[unroll]
					for (int i = 0; i < 3; i++) {
						index = input[i].id + offset;
						input[i].wposition = getPosition(index);
						input[i].position = UnityWorldToClipPos(input[i].wposition);
						pointStream.Append(input[i]);
					}
				}

				float4 frag(v2f i) : SV_Target
				{
					int index = i.id;
					float2 uv = IndexToUV(index, _Buffer_TexelSize);
					uv.y += .5f;
					float4 trajectory = tex2Dlod(_Buffer, float4(uv, 0, 0));

					float4 output = float4(0,0,0,0);
					float4 col = tex2D(_MainTex, i.uv);
					float l = length(trajectory.xyz);
					col = shiftColor(col, l * 5);

					//brightness
					float3 cameraDir = i.wposition - _WorldSpaceCameraPos;
					float brightness = 5 / ( 1 + dot(cameraDir,cameraDir));
					output = col * brightness;
					return output;
				}
				ENDCG
			}
		}
}


