#ifndef GRASS_BEZIER_INCLUDED
#define GRASS_BEZIER_INCLUDED

// 三次贝塞尔：P0, P1, P2, P3, t in [0,1]
float3 Bezier3(float3 p0, float3 p1, float3 p2, float3 p3, float t)
{
    float u   = 1.0 - t;
    float tt  = t * t;
    float uu  = u * u;
    float uuu = uu * u;
    float ttt = tt * t;

    // B(t) = u^3 * P0 + 3u^2 t P1 + 3u t^2 P2 + t^3 P3
    return uuu * p0 +
           3.0 * uu * t * p1 +
           3.0 * u * tt * p2 +
           ttt * p3;
}

// 贝塞尔切线（用于法线、方向等）
float3 Bezier3Tangent(float3 p0, float3 p1, float3 p2, float3 p3, float t)
{
    // B'(t) = 3(1-t)^2 (P1-P0) + 6(1-t)t (P2-P1) + 3t^2 (P3-P2)
    float u  = 1.0 - t;
    float tt = t * t;
    float uu = u * u;

    float3 term0 = 3.0 * uu * (p1 - p0);
    float3 term1 = 6.0 * u * t * (p2 - p1);
    float3 term2 = 3.0 * tt * (p3 - p2);

    return normalize(term0 + term1 + term2);
}

#endif // GRASS_BEZIER_INCLUDED
