Shader "HairStudio"
{
	Properties
	{
		// Diffuse
		[Header(Diffuse feature)]
		[Tooltip(The color of the diffuse feature. Realistic is less saturated than both second reflection and transmittance.)]
		_ColorD("Color D", Color) = (0,0,0,1)
		[Tooltip(The intensity of the diffuse feature. Set the minimum value for realistic override.)]
		_ScaleD("Intensity D", Range(0, 1)) = 0

		// R
		[Space]
		[Header(reflection feature (Marschner R))]
		[Tooltip(The color of the reflection feature. Realistic is white as the light bounces on the surface of the hair.)]
		_ColorR("Color R", Color) = (0, 0, 0, 1)

		[Tooltip(The intensity of the reflection feature. Represent the light absorption. The sum of the intensities of all features should not be greater than 1 (or you would create light out of nowhere).)]
		_ScaleR("Intensity R", Range(0, 1)) = 0.33

		[Tooltip(The angle of the reflection feature. Realistic is between minus 10 and minus 5 degrees.)]
		_AlphaR("Angle (alpha R)", Range(-20, 20)) = -7.5

		[Tooltip(The width of the reflection feature. Realistic is between 5 and 10 degrees.)] 
		_BetaR("Width (beta R)", Range(0, 20)) = 7.5

		// TT
		[Space]
		[Header(Transmittance feature (Marschner TT))]
		[Tooltip(The color of the transmittance feature. Realistic is more saturated than reflection but less than second reflection.)]
		_ColorTT("Color TT", Color) = (0, 0, 0, 1)
			
		[Tooltip(The intensity of the transmittance feature. Set the minimum value for realistic override (Intensity R x 3).)]
		_ScaleTT("Intensity TT", Range(0, 1)) = 0

		[Tooltip(The angle of the transmittance feature. Set the minimum value for realistic override (minus Angle R x 0.5).)]
		_AlphaTT("Angle (alpha TT)", Range(-20, 20)) = 0

		[Tooltip(The width of the transmittance feature. Set the minimum value for realistic override (Width R x 0.5).)]
		_BetaTT("Width (beta TT)", Range(0, 20)) = 0

		[Tooltip(The width of the transmittance feature perpendicularly to the direction of the hair.)]
		_GammaTT("Azimuthal width (gamma TT)", Range(0.001, 50)) = 2

		// TRT
		[Space]
		[Header(Second reflection feature (Marschner TRT))]
		[Tooltip(The color of the second reflection feature. Consider it as the base color of the hair.)]
		_ColorTRT("Color TRT", Color) = (0, 0, 0, 1)

		[Tooltip(The intensity of the second reflection feature. Set the minimum value for realistic override (Intensity R x 0.5).)]
		_ScaleTRT("Intensity TRT", Range(0, 1)) = 0

		[Tooltip(The angle of the second reflection feature. Set the minimum value for realistic override (minus Angle R x 1.5).)]
		_AlphaTRT("Angle (alpha TRT)", Range(-20, 20)) = 0

		[Tooltip(The width of the second reflection feature. Set the minimum value for realistic override (Width R x 2).)]
		_BetaTRT("Width (beta TRT)", Range(0, 20)) = 0 

		// G
		[Space]
		[Header(Glints (Marschner G))]
		[Tooltip(The color of the glints. Realistic is more saturated than second reflection.)]
		_ColorG("Color G", Color) = (0, 0, 0, 1)

		[Tooltip(The intensity of the glints. Set the minimum value for realistic override (Intensity R x 2).)]
		_ScaleG("Scale G", Range(0, 1)) = 0

		[Tooltip(The width of the glints perpendicularly to the direction of the hair. Realistic is between 10 and 25 degrees.)]
		_GammaG("Azimuthal width (gamma G)", Range(0, 30)) = 2

		[Tooltip(The half angle between the two glints. A higher value will separate the left and right glints.)]
		_PhiG("Separation angle (phi G)", Range(0, 40)) = 2

		// other
		[Space]
		[Header(Hair shape)]
		[Tooltip(A multiplier for all colors applied differently on each strand to add noise on the overall color.)]
		_ColorNuance("Hair color nuance", Range(0, 0.1)) = 0.01

		[Tooltip(The thickness of the hair strands at the root.)]
		_ThicknessRoot("Thickness at root", Range(0, 0.01)) = 0.002

		[Tooltip(The thickness of the hair strands at the tip.)]
		_ThicknessTip("Thickness at tip", Range(0, 0.01)) = 0

		[Tooltip(The distance from the root at which the thickness starts to decrease.)]
		_ThicknessDecreaseRate("Thickness decrease distance", Range(0, 1)) = 0.9

		//_SelfShadowStrength("_SelfShadowStrength", Range(0, 1)) = 0.1
	}
	SubShader
	{
		CGPROGRAM
		#include "HairStudio_MarschnerLightingModel.cginc"
		#pragma surface surf Marschner vertex:VertBase addshadow fullforwardshadows
		
		#pragma target 4.5

		struct SegmentForShading
		{
			float3 pos;
			float3 frame;
			float3 tangent;
			float3 up;
		};

	    struct SegmentDef
		{
			float3 initialLocalPos;
			float roughnessRate;
			float eccentricityRate;
		};

		int _FirstSegmentIndex;
		float3 _Offset;

#if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_MOBILE)
		StructuredBuffer<SegmentForShading> _SegmentsForShading;
		StructuredBuffer<SegmentDef> _SegmentDefinitions; 
#endif

		struct Input {
			float3 wPos;
			float3 iTangent;
			int index;
			float random;
			float2 uv_HairColorTex;
		};

		uniform float _ThicknessRoot;
		uniform float _ThicknessDecreaseRate;
		uniform float _ThicknessTip;
		
		// MISC
		uniform int _VerticesPerStrand;
				
		float3 GetBezier(float3 p0, float3 t0, float3 p1, float3 t1, float t) {
			float3 d0 = p0 + t0 * 0.01f;
			float3 d1 = p1 - t1 * 0.01f;

			float omt = 1.0f - t;
			float omt2 = omt * omt;
			float t2 = t * t;
			return
				p0 * (omt2 * omt) +
				d0 * (3.0f * omt2 * t) +
				d1 * (3.0f * omt * t2) +
				p1 * (t2 * t);
		}

		float3 GetTangent(float3 p0, float3 t0, float3 p1, float3 t1, float t) {
			float3 d0 = p0 + t0 * 0.01f;
			float3 d1 = p1 - t1 * 0.01f;

			float omt = 1.0f - t;
			float omt2 = omt * omt;
			float t2 = t * t;
			float3 tangent =
				p0 * (-omt2) +
				d0 * (3 * omt2 - 2 * omt) +
				d1 * (-3 * t2 + 2 * t) +
				p1 * (t2);
			return normalize(tangent);
		}

		void VertBase (inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input,o);
			float strandIndex01 = v.vertex.x;
            uint segmentIndex = v.normal.x;
            float segmentRate = v.normal.y;
            int segmentSide = v.normal.z;
			float curveTime = v.vertex.y;
			SegmentForShading seg = { {0, 0, 0}, {0, 0, 0}, {0, 0, 0}, {0, 0, 0} };
			SegmentDef def = { {0, 0, 0}, 0, 0 };
#if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_MOBILE)
			seg = _SegmentsForShading[_FirstSegmentIndex + segmentIndex];
			def = _SegmentDefinitions[segmentIndex];
#endif

			// finding the vertex on the strand curve
			float3 posOnCurve = seg.pos;
			float3 tangentOnCurve = seg.tangent;
			if (curveTime != 0) {
				SegmentForShading next = { {0, 0, 0}, {0, 0, 0}, {0, 0, 0}, {0, 0, 0} };
#if defined(SHADER_API_D3D11) || defined(SHADER_API_METAL) || defined(SHADER_API_MOBILE)
				next = _SegmentsForShading[_FirstSegmentIndex + segmentIndex + 1];
#endif	
				posOnCurve = GetBezier(seg.pos, (seg.frame + seg.tangent) / 2, next.pos, (next.frame + next.tangent) / 2, curveTime); 
				tangentOnCurve = GetTangent(seg.pos, (seg.frame + seg.tangent) / 2, next.pos, (next.frame + next.tangent) / 2, curveTime);
			}

			// debug to draw something if the data is not set
			if (posOnCurve.x == 0 && posOnCurve.y == 0 && posOnCurve.z == 0) {
				float offset2 = ((float)segmentIndex) * 0.001f;
				posOnCurve = float3(offset2, offset2, -offset2);
			}

			if (tangentOnCurve.x == 0 && tangentOnCurve.y == 0 && tangentOnCurve.z == 0) {
				tangentOnCurve = float3(1, 1, -1);
			}



			// hair width offset
			float localThickness = lerp(_ThicknessRoot, _ThicknessTip, saturate(invLerp(_ThicknessDecreaseRate, 1, segmentRate)));
            float offset = (localThickness / 2) * segmentSide;

			float3 viewToSeg = normalize(posOnCurve - _WorldSpaceCameraPos);
			float3 right = normalize(cross(viewToSeg, normalize(tangentOnCurve)));

			float3 vertexPos = posOnCurve - _Offset + right * offset;

			v.vertex.xyz = vertexPos;
			v.normal = float3(0, 1, 0);
			//v.texcoord = float4(0, segmentRate, 0, 0);

			o.index = segmentIndex;
			o.iTangent = tangentOnCurve;
			o.random = strandIndex01;
		}
		
		void surf (Input IN, inout SurfaceOutputHair o)
		{
			o.random = IN.random;
			o.iTangent = IN.iTangent;
		}
		ENDCG
	}

	SubShader
	{
		Pass
		{
			 Name "ShadowCaster"
			 Tags { "LightMode" = "ShadowCaster" }

			 Fog { Mode Off }
			 ZWrite On ZTest Less Cull Off
			 Offset 1, 1

			 CGPROGRAM

			 #pragma vertex vert
			 #pragma fragment frag
			 #pragma multi_compile_shadowcaster
			 #pragma fragmentoption ARB_precision_hint_fastest

			 #include "UnityCG.cginc"

			 sampler2D _MainTex;

			 struct v2f
			 {
				 V2F_SHADOW_CASTER;
				 half2 uv:TEXCOORD1;
			 };

			 v2f vert(appdata_base v)
			 {
				 v2f o;
				 o.uv = v.texcoord;
				 TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				 return o;
			 }

			 float4 frag(v2f i) : COLOR
			 {
				 fixed alpha = tex2D(_MainTex, i.uv).a;
				 clip(alpha - 0.5f);
				 SHADOW_CASTER_FRAGMENT(i)
			 }

			 ENDCG
		}
	}
	Fallback "Diffuse"
	Fallback "VertexLit"
}