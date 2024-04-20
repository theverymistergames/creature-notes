Shader "Unlit/Pulse"
{
    Properties
    {
        [HDR] [MainColor] _MainColor ("Main Color", Color) = (0.0, 0.0, 0.0, 255.0)
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        _brightness("Brightness", float) = 2

        _scale("Scale", float) = 6.0
        _scaleStep("Scale Step", float) = 1.2
        _rotationStep("Rotation Step", float) = 5

        _iterations("Iterations", float) = 16
        _uvAnimationSpeed("Animation Speed", float) = 3.5

        [HDR] _MaskColor ("Mask Color", Color) = (0.0, 0.0, 0.0, 255.0)
        _MaskTex("Mask Texture", 2D) = "white" {}
        _maskIterations("Mask Iterations", float) = 8
        _maskStrength("Mask Strength", float) = 0
        _maskScale("Mask Scale", float) = 6.0
        _maskScaleStep("Mask Scale Step", float) = 1.2
        _maskBrightness("Mask Brightness", float) = 2

        _rippleStrength("Ripple Strength", float) = 0.9
        _rippleMaxFrequency("Ripple Max Frequency", float) = 1.4
        _rippleSpeed("Ripple Speed", float) = 5
    }
    SubShader
    {
        Tags{ "RenderPipeline"="HDRenderPipeline" "RenderType" = "Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            float4 _MainColor;
            sampler2D _MainTex;
            float _brightness;

            float _scale;
            float _scaleStep;
            float _rotationStep;

            float _iterations;
            float _uvAnimationSpeed;

            float4 _MaskColor;
            sampler2D _MaskTex;
            float _maskBrightness;

            float _maskIterations;
            float _maskStrength;
            float _maskScale;
            float _maskScaleStep;

            float _rippleStrength;
            float _rippleMaxFrequency;
            float _rippleSpeed;

            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            // Get 2D rotation matrix given rotation in degrees.
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float2x2 Get2DRotationMatrix(const float angle)
            {
                return float2x2(cos(angle), sin(angle), -sin(angle), cos(angle));
            }

            float GetRipples(
                const float2 uv,
                const float t,
                const float ripple_strength,
                const float ripple_max_frequency,
                const float ripple_speed)
            {
                const float l = length(uv);
                const float inverted_radial_gradient = l * l;

                return sin(t * ripple_speed - inverted_radial_gradient * ripple_max_frequency) * ripple_strength;
            }

            // Output this function directly (default values only for reference).
            float GetOrganicFractal_float(
                float2 uv,
                const float t,
                float scale,
                const float scale_mult_step,
                const float rotation_step,
                const int iterations,
                const float uv_animation_speed,
                const float ripples,
                const float brightness)
            {
                // Remap to [-1.0, 1.0].
                uv = float2(uv - 0.5) * 2.0;

                float2 n;
                float output = 0.0;
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

                    scale *= scale_mult_step;
                }

                return output;
            }

            fixed4 frag (const v2f i) : SV_Target {
                // sample the texture
                const fixed4 main_tex_sample = tex2D(_MainTex, i.uv);
                const fixed4 main = main_tex_sample * _MainColor;

                const fixed4 mask_tex_sample = tex2D(_MaskTex, i.uv);
                const fixed4 mask = mask_tex_sample * _MaskColor;

                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, base);
                UNITY_APPLY_FOG(i.fogCoord, main);

                const float t = _Time.y;
                const float ripples = GetRipples(i.uv, t, _rippleStrength, _rippleMaxFrequency, _rippleSpeed);

                const float f = GetOrganicFractal_float(
                    i.uv,
                    t,
                    _scale,
                    _scaleStep,
                    _rotationStep,
                    _iterations,
                    _uvAnimationSpeed,
                    ripples,
                    _brightness
                );

                const float f_mask = GetOrganicFractal_float(
                    i.uv,
                    t,
                    _maskScale,
                    _maskScaleStep,
                    _rotationStep,
                    _maskIterations,
                    _uvAnimationSpeed,
                    ripples,
                    _maskBrightness
                );

                return main * f + mask * f_mask * _maskStrength;
            }
            ENDCG
        }
    }
}
