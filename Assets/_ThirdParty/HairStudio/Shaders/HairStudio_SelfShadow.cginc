#ifndef HairStudio_SelfShadow
#define HairStudio_SelfShadow

#define SHADOW_E 2.71828183 // Shadow epsilon
#define KERNEL_SIZE 5 // SelfShadow PCF filter range

// Projection informations
uniform float4x4 _SelfShadowMatrix;

// Texture fake point
uniform sampler2D _TextureFakePoint;

// Shadow map
uniform sampler2D _SelfShadowMap;
uniform float _SelfShadowFiberSpacing;

//--------------------------------------------------------------------------------------
// ComputeShadow
//
// Computes the shadow using a simplified deep shadow map technique for the hair and
// PCF for scene objects. It uses multiple taps to filter over a (KERNEL_SIZE x KERNEL_SIZE)
// kernel for high quality results.
//--------------------------------------------------------------------------------------
float ComputeShadow(float3 worldPos, float alpha, float width, float widthMult)
{
    float4 projPosLight = mul(_SelfShadowMatrix, float4(worldPos, 1));

    float2 texSM = projPosLight.xy;
    float depth_fragment = 1 - (projPosLight.z);

	// for shadow casted by scene objs, use PCF shadow
    float total_weight = 0;
    float amountLight_hair = 0;
    float dist = tex2D(_TextureFakePoint, float2(0, 0)).a;

    total_weight = 0;
	[unroll]
    for (int dx = (1 - KERNEL_SIZE) / 2; dx <= KERNEL_SIZE / 2; dx++)
    {
		[unroll]
        for (int dy = (1 - KERNEL_SIZE) / 2; dy <= KERNEL_SIZE / 2; dy++)
        {
            float size = 2.4;
            float sigma = (KERNEL_SIZE / 2.0) / size; // standard deviation, when kernel/2 > 3*sigma, it's close to zero, here we use 1.5 instead
            float exp = -1 * (dx * dx + dy * dy) / (2 * sigma * sigma);
            float weight = 1 / (2 * 3.1415926 * sigma * sigma) * pow(SHADOW_E, exp);

			// shadow casted by hair: simplified deep shadow map
            float depthSMHair = 1 - tex2D(_SelfShadowMap, texSM + float2(float(dx) / 2048, float(dy) / 2048)).x; //z/w

            float depth_range = max(0, depth_fragment - depthSMHair);
            float numFibers = depth_range / (_SelfShadowFiberSpacing * (width * widthMult));

			// if occluded by hair, there is at least one fiber
			[flatten]
            if (depth_range > 1e-5)
                numFibers += 1;
            amountLight_hair += pow(0.5, numFibers) * weight;

            total_weight += weight;
        }
    }
    amountLight_hair /= total_weight;

    return amountLight_hair;

    }

#endif