#ifndef HairStudio_Math
#define HairStudio_Math

static const float pi = 3.1415926;
static const float epsilon = 0.00001;
static const float4 quaternionIdentity = float4(0, 0, 0, 1);
static const float3 _up = float3(0, 1, 0);
static const float3 _right = float3(1, 0, 0);
static const float3 _forward = float3(0, 0, 1);

float invLerp(float from, float to, float value)
{
	return (value - from) / (to - from);
}

float4 permute(float4 x)
{
    return fmod(((x * 34.0) + 1.0) * x, 289.0);
}

float4 taylorInvSqrt(float4 r)
{
    return 1.79284291400159 - 0.85373472095314 * r;
}

float simplexNoise(float3 v)
{
    const float2 C = float2(1.0 / 6.0, 1.0 / 3.0);
    const float4 D = float4(0.0, 0.5, 1.0, 2.0);

// First corner
    float3 i = floor(v + dot(v, C.yyy));
    float3 x0 = v - i + dot(i, C.xxx);

// Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1.0 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);

  //  x0 = x0 - 0. + 0.0 * C 
    float3 x1 = x0 - i1 + 1.0 * C.xxx;
    float3 x2 = x0 - i2 + 2.0 * C.xxx;
    float3 x3 = x0 - 1. + 3.0 * C.xxx;

// Permutations
    i = fmod(i, 289.0);
    float4 p = permute(permute(permute(
             i.z + float4(0.0, i1.z, i2.z, 1.0))
           + i.y + float4(0.0, i1.y, i2.y, 1.0))
           + i.x + float4(0.0, i1.x, i2.x, 1.0));

// Gradients
// ( N*N points uniformly over a square, mapped onto an octahedron.)
    float n_ = 1.0 / 7.0; // N=7
    float3 ns = n_ * D.wyz - D.xzx;

    float4 j = p - 49.0 * floor(p * ns.z * ns.z); //  mod(p,N*N)

    float4 x_ = floor(j * ns.z);
    float4 y_ = floor(j - 7.0 * x_); // mod(j,N)

    float4 x = x_ * ns.x + ns.yyyy;
    float4 y = y_ * ns.x + ns.yyyy;
    float4 h = 1.0 - abs(x) - abs(y);

    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);

    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, float4(0, 0, 0, 0));

    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;

    float3 p0 = float3(a0.xy, h.x);
    float3 p1 = float3(a0.zw, h.y);
    float3 p2 = float3(a1.xy, h.z);
    float3 p3 = float3(a1.zw, h.w);

//Normalise gradients
    float4 norm = taylorInvSqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;

// Mix final noise value
    float4 m = max(0.6 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    m = m * m;
    return 42.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1),
                                dot(p2, x2), dot(p3, x3)));
}

//	Classic Perlin 3D Noise 
//	by Stefan Gustavson
//
float3 fade(float3 t)
{
    return t * t * t * (t * (t * 6.0 - 15.0) + 10.0);
}

float cnoise(float3 P)
{
    float3 Pi0 = floor(P); // Integer part for indexing
    float3 Pi1 = Pi0 + float3(1, 1, 1); // Integer part + 1
    Pi0 = fmod(Pi0, 289.0);
    Pi1 = fmod(Pi1, 289.0);
    float3 Pf0 = frac(P); // Fractional part for interpolation
    float3 Pf1 = Pf0 - float3(1, 1, 1); // Fractional part - 1.0
    float4 ix = float4(Pi0.x, Pi1.x, Pi0.x, Pi1.x);
    float4 iy = float4(Pi0.yy, Pi1.yy);
    float4 iz0 = Pi0.zzzz;
    float4 iz1 = Pi1.zzzz;

    float4 ixy = permute(permute(ix) + iy);
    float4 ixy0 = permute(ixy + iz0);
    float4 ixy1 = permute(ixy + iz1);

    float4 gx0 = ixy0 / 7.0;
    float4 gy0 = frac(floor(gx0) / 7.0) - 0.5;
    gx0 = frac(gx0);
    float4 gz0 = float4(0.5, 0.5, 0.5, 0.5) - abs(gx0) - abs(gy0);
    float4 sz0 = step(gz0, float4(0, 0, 0, 0));
    gx0 -= sz0 * (step(0.0, gx0) - 0.5);
    gy0 -= sz0 * (step(0.0, gy0) - 0.5);

    float4 gx1 = ixy1 / 7.0;
    float4 gy1 = frac(floor(gx1) / 7.0) - 0.5;
    gx1 = frac(gx1);
    float4 gz1 = float4(0.5, 0.5, 0.5, 0.5) - abs(gx1) - abs(gy1);
    float4 sz1 = step(gz1, float4(0, 0, 0, 0));
    gx1 -= sz1 * (step(0.0, gx1) - 0.5);
    gy1 -= sz1 * (step(0.0, gy1) - 0.5);

    float3 g000 = float3(gx0.x, gy0.x, gz0.x);
    float3 g100 = float3(gx0.y, gy0.y, gz0.y);
    float3 g010 = float3(gx0.z, gy0.z, gz0.z);
    float3 g110 = float3(gx0.w, gy0.w, gz0.w);
    float3 g001 = float3(gx1.x, gy1.x, gz1.x);
    float3 g101 = float3(gx1.y, gy1.y, gz1.y);
    float3 g011 = float3(gx1.z, gy1.z, gz1.z);
    float3 g111 = float3(gx1.w, gy1.w, gz1.w);

    float4 norm0 = taylorInvSqrt(float4(dot(g000, g000), dot(g010, g010), dot(g100, g100), dot(g110, g110)));
    g000 *= norm0.x;
    g010 *= norm0.y;
    g100 *= norm0.z;
    g110 *= norm0.w;
    float4 norm1 = taylorInvSqrt(float4(dot(g001, g001), dot(g011, g011), dot(g101, g101), dot(g111, g111)));
    g001 *= norm1.x;
    g011 *= norm1.y;
    g101 *= norm1.z;
    g111 *= norm1.w;

    float n000 = dot(g000, Pf0);
    float n100 = dot(g100, float3(Pf1.x, Pf0.yz));
    float n010 = dot(g010, float3(Pf0.x, Pf1.y, Pf0.z));
    float n110 = dot(g110, float3(Pf1.xy, Pf0.z));
    float n001 = dot(g001, float3(Pf0.xy, Pf1.z));
    float n101 = dot(g101, float3(Pf1.x, Pf0.y, Pf1.z));
    float n011 = dot(g011, float3(Pf0.x, Pf1.yz));
    float n111 = dot(g111, Pf1);

    float3 fade_xyz = fade(Pf0);
    float4 n_z = lerp(float4(n000, n100, n010, n110), float4(n001, n101, n011, n111), fade_xyz.z);
    float2 n_yz = lerp(n_z.xy, n_z.zw, fade_xyz.y);
    float n_xyz = lerp(n_yz.x, n_yz.y, fade_xyz.x);
    return 2.2 * n_xyz;
}

inline float square(float x)
{
    return x * x;
}

float SqrLength(float3 v)
{
	return v.x * v.x + v.y * v.y + v.z * v.z;
}

float3 MultQV(float4 q, float3 v)
{
	float3 uv, uuv;
	float3 qvec = float3(q.x, q.y, q.z);
	uv = cross(qvec, v);
	uuv = cross(qvec, uv);
	uv *= (2.0f * q.w);
	uuv *= 2.0f;
	return v + uv + uuv;
}

float4 MultQQ(float4 qA, float4 qB)
{
	float4 q;
	q.w = qA.w * qB.w - qA.x * qB.x - qA.y * qB.y - qA.z * qB.z;
	q.x = qA.w * qB.x + qA.x * qB.w + qA.y * qB.z - qA.z * qB.y;
	q.y = qA.w * qB.y + qA.y * qB.w + qA.z * qB.x - qA.x * qB.z;
	q.z = qA.w * qB.z + qA.z * qB.w + qA.x * qB.y - qA.y * qB.x;
	return q;
}

//float4 qmul(float4 q1, float4 q2)
//{
//	return float4(
//        q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
//        q1.w * q2.w - dot(q1.xyz, q2.xyz)
//    );
//}

//float3 rotate_vector(float4 r, float3 v)
//{
//    float4 r_c = r * float4(-1, -1, -1, 1);
//    return qmul(r, qmul(float4(v, 0), r_c)).xyz;
//}


//float4 MulQuaternions(float4 q1, float4 q2)
//{
//	float4 res;
//	res.w = q1.w * q2.w - dot(q1.xyz, q2.xyz);
//	res.xyz = q1.w * q2.xyz + q2.w * q1.xyz + cross(q1.xyz, q2.xyz);
//	return res;
//}

//float3 MulQuaternionVector(in float4 q, in float3 v)
//{
//	float3 t = 2.0 * cross(q.xyz, v);
//	return v + q.w * t + cross(q.xyz, t);
//}

float4 QuaternionLookAt(float3 forward, float3 up)
{
	forward = normalize(forward);
	float3 right = normalize(cross(up, forward));
	up = cross(forward, right);

	float m00 = right.x;
	float m01 = right.y;
	float m02 = right.z;
	float m10 = up.x;
	float m11 = up.y;
	float m12 = up.z;
	float m20 = forward.x;
	float m21 = forward.y;
	float m22 = forward.z;

	float num8 = (m00 + m11) + m22;
	float4 q = quaternionIdentity;
	if (num8 > 0.0)
	{
		float num = sqrt(num8 + 1.0);
		q.w = num * 0.5;
		num = 0.5 / num;
		q.x = (m12 - m21) * num;
		q.y = (m20 - m02) * num;
		q.z = (m01 - m10) * num;
		return q;
	}

	if ((m00 >= m11) && (m00 >= m22))
	{
		float num7 = sqrt(((1.0 + m00) - m11) - m22);
		float num4 = 0.5 / num7;
		q.x = 0.5 * num7;
		q.y = (m01 + m10) * num4;
		q.z = (m02 + m20) * num4;
		q.w = (m12 - m21) * num4;
		return q;
	}

	if (m11 > m22)
	{
		float num6 = sqrt(((1.0 + m11) - m00) - m22);
		float num3 = 0.5 / num6;
		q.x = (m10 + m01) * num3;
		q.y = 0.5 * num6;
		q.z = (m21 + m12) * num3;
		q.w = (m20 - m02) * num3;
		return q;
	}

	float num5 = sqrt(((1.0 + m22) - m00) - m11);
	float num2 = 0.5 / num5;
	q.x = (m20 + m02) * num2;
	q.y = (m21 + m12) * num2;
	q.z = 0.5 * num5;
	q.w = (m01 - m10) * num2;
	return q;
}
#endif