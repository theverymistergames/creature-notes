//UNITY_SHADER_NO_UPGRADE
#ifndef RIPPLES
#define RIPPLES

void GetRipples_float(
    const float2 uv,
    const float t,
    const float multiplier,
    const float max_frequency,
    const float speed,
    out float output
) {
    const float l = length(uv);
    const float inverted_radial_gradient = l * l;

    output = sin(t * speed - inverted_radial_gradient * max_frequency) * multiplier;
}

void GetRipples_half(
    const half2 uv,
    const half t,
    const half multiplier,
    const half max_frequency,
    const half speed,
    out half output
) {
    const half l = length(uv);
    const half inverted_radial_gradient = l * l;

    output = sin(t * speed - inverted_radial_gradient * max_frequency) * multiplier;
}

#endif //RIPPLES
