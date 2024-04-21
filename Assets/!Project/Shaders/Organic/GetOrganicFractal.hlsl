//UNITY_SHADER_NO_UPGRADE
#ifndef ORGANIC_FRACTAL
#define ORGANIC_FRACTAL

float2x2 Get2DRotationMatrix(const float angle)
{
    return float2x2(cos(angle), sin(angle), -sin(angle), cos(angle));
}

half2x2 Get2DRotationMatrix(const half angle)
{
    return half2x2(cos(angle), sin(angle), -sin(angle), cos(angle));
}

void GetOrganicFractal_float(
    float2 uv,
    const float t,
    float scale,
    const float scale_multiplication_step,
    const float rotation_step,
    const float iterations,
    const float uv_animation_speed,
    const float ripples,
    const float brightness,
    out float output
) {
    // Remap to [-1.0, 1.0].
    uv = float2(uv - 0.5) * 2.0;

    float2 n;
    output = 0.0;

    const float2x2 rotation_matrix = Get2DRotationMatrix(rotation_step);
    const float uv_time = t * uv_animation_speed;

    for (int i = 0; i < iterations; i++)
    {
        uv = mul(rotation_matrix, uv);
        n = mul(rotation_matrix, n);

        const float2 animated_uv = uv * scale + uv_time;
        const float2 q = animated_uv + ripples + i + n;

        output += dot(cos(q) / scale, float2(1.0, 1.0) * brightness);

        n -= sin(q);

        scale *= scale_multiplication_step;
    }
}

void GetOrganicFractal_half(
    half2 uv,
    const half t,
    half scale,
    const half scale_multiplication_step,
    const half rotation_step,
    const half iterations,
    const half uv_animation_speed,
    const half ripples,
    const half brightness,
    out half output
) {
    // Remap to [-1.0, 1.0].
    uv = half2(uv - 0.5) * 2.0;

    half2 n;
    output = 0.0;

    const half2x2 rotation_matrix = Get2DRotationMatrix(rotation_step);
    const half uv_time = t * uv_animation_speed;

    for (int i = 0; i < iterations; i++)
    {
        uv = mul(rotation_matrix, uv);
        n = mul(rotation_matrix, n);

        const half2 animated_uv = uv * scale + uv_time;
        const half2 q = animated_uv + ripples + i + n;

        output += dot(cos(q) / scale, half2(1.0, 1.0) * brightness);

        n -= sin(q);

        scale *= scale_multiplication_step;
    }
}

#endif //ORGANIC_FRACTAL
