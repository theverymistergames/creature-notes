using System.Collections.Generic;
using MisterGames.Common;
using MisterGames.Logic.Rendering;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshVertexPosition : MonoBehaviour {

        [SerializeField] private MeshHeightData _meshHeightData;
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private float _fractalMul = 0.434f;
        [SerializeField] private float _alphaPowR = 2.15f;
        
        private static readonly int RipplesStrength = Shader.PropertyToID("_Ripples_Strength"); 
        private static readonly int RipplesMaxFrequency = Shader.PropertyToID("_Ripples_Max_Frequency"); 
        private static readonly int RipplesSpeed = Shader.PropertyToID("_Ripples_Speed");
        private static readonly int FractalTiling = Shader.PropertyToID("_Fractal_Tiling");
        private static readonly int FractalOffset = Shader.PropertyToID("_Fractal_Offset");
        private static readonly int AlphaTiling = Shader.PropertyToID("_Alpha_Tiling");
        private static readonly int AlphaOffset = Shader.PropertyToID("_Alpha_Offset");
        private static readonly int IntensityVector = Shader.PropertyToID("_IntensityVector");
        private static readonly int PositionOffset = Shader.PropertyToID("_Position_Offset");
        private static readonly int RotationStep = Shader.PropertyToID("_Rotation_Step");
        private static readonly int UvAnimationSpeed = Shader.PropertyToID("_Uv_Animation_Speed");
        private static readonly int FractalStrength = Shader.PropertyToID("_Fractal_Strength");
        private static readonly int Scale = Shader.PropertyToID("_Scale");
        private static readonly int ScaleStep = Shader.PropertyToID("_Scale_Step");
        private static readonly int Iterations = Shader.PropertyToID("_Iterations");
        private static readonly int Brightness = Shader.PropertyToID("_Brightness");
        private static readonly int Displacement = Shader.PropertyToID("_Displacement");
        private static readonly int MaskStrength = Shader.PropertyToID("_Mask_Strength");
        private static readonly int MaskScale = Shader.PropertyToID("_Mask_Scale");
        private static readonly int MaskScaleStep = Shader.PropertyToID("_Mask_Scale_Step");
        private static readonly int MaskIterations = Shader.PropertyToID("_Mask_Iterations");
        private static readonly int MaskBrightness = Shader.PropertyToID("_Mask_Brightness");
        private static readonly int MaskDisplacement = Shader.PropertyToID("_Mask_Displacement");
        private static readonly int AlphaMask = Shader.PropertyToID("_Alpha_Mask");
        private static readonly int Time1 = Shader.PropertyToID("_Time");
        
        private Transform _transform;
        private Bounds _bounds;
        
        private void Awake() {
            _transform = _meshFilter.transform;
            _bounds = _meshRenderer.localBounds;
        }

        public bool TrySamplePosition(ref Vector3 worldPosition) {
            if (!_meshHeightData.TrySamplePosition(ref worldPosition)) return false;
            
            var center = _bounds.center;
            worldPosition = _transform.TransformPoint(Sample(_transform.InverseTransformPoint(worldPosition) - center) + center);

#if UNITY_EDITOR
            if (_showSamples) {
                DebugExt.DrawCircle(worldPosition, _transform.rotation, 0.05f, Color.green);
                DebugExt.DrawRay(worldPosition, _transform.up * 0.005f, Color.green);
            }
#endif
            
            return true;
        }
        
        private Vector3 Sample(Vector3 point) {
#if UNITY_EDITOR
            var mat = Application.isPlaying ? _meshRenderer.material : _meshRenderer.sharedMaterial;
#else
            var mat = _meshRenderer.material;
#endif

            float time = Shader.GetGlobalVector(Time1).y;
            
            float ripplesStrength = mat.GetFloat(RipplesStrength);
            float ripplesMaxFrequency = mat.GetFloat(RipplesMaxFrequency);
            float ripplesSpeed = mat.GetFloat(RipplesSpeed);
            
            Vector2 fractalTiling = mat.GetVector(FractalTiling);
            Vector2 fractalOffset = mat.GetVector(FractalOffset);
            
            float rotationStep = mat.GetFloat(RotationStep);
            float uvAnimationSpeed  = mat.GetFloat(UvAnimationSpeed);
            
            float fractalStrength = mat.GetFloat(FractalStrength);
            float fractalScale = mat.GetFloat(Scale);
            float fractalScaleStep = mat.GetFloat(ScaleStep);
            float fractalIterations = mat.GetFloat(Iterations);
            float fractalBrightness = mat.GetFloat(Brightness);
            float displacement = mat.GetFloat(Displacement);
            
            float maskStrength = mat.GetFloat(MaskStrength);
            float maskScale = mat.GetFloat(MaskScale);
            float maskScaleStep = mat.GetFloat(MaskScaleStep);
            float maskIterations = mat.GetFloat(MaskIterations);
            float maskBrightness = mat.GetFloat(MaskBrightness);
            float maskDisplacement = mat.GetFloat(MaskDisplacement);

            var alphaMask = (Texture2D) mat.GetTexture(AlphaMask);
            Vector2 alphaTiling = mat.GetVector(AlphaTiling);
            Vector2 alphaOffset = mat.GetVector(AlphaOffset);
           
            Vector3 intensityVector = mat.GetVector(IntensityVector);
            Vector3 positionOffset = mat.GetVector(PositionOffset);

            var uv = new float2(point.x, point.z); 
            
            float ripples = GetRipples(uv, time, ripplesStrength, ripplesMaxFrequency, ripplesSpeed);

            var fractalUv = TilingAndOffset(uv, fractalTiling, fractalOffset);
            float t = time * uvAnimationSpeed ;
            float fractal = GetOrganicFractal(fractalUv, t, fractalScale, fractalScaleStep, rotationStep, fractalIterations, ripples, fractalBrightness) * fractalStrength;
            float fractalMask = GetOrganicFractal(fractalUv, t, maskScale, maskScaleStep, rotationStep, maskIterations, ripples, maskBrightness) * maskStrength;

            float fractalInput = _fractalMul * (fractal * displacement + fractalMask * maskDisplacement);

            var alphaMaskColor = SampleTexture2D(alphaMask, TilingAndOffset(uv, alphaTiling, alphaOffset));

            var normal = Vector3.up;

            var lerp = new Vector3(
                Mathf.Lerp(point.x, fractalInput * normal.x, alphaMaskColor.r * intensityVector.x),
                Mathf.Lerp(point.y, fractalInput * normal.y, alphaMaskColor.r * intensityVector.y),
                Mathf.Lerp(point.z, fractalInput * normal.z, alphaMaskColor.r * intensityVector.z)
            );

            return lerp + positionOffset * pow(alphaMaskColor.r, _alphaPowR);
        }

        private static Color SampleTexture2D(Texture2D texture, Vector2 uv) {
            return texture.GetPixelBilinear(uv.x, uv.y);
        }
        
        private static Vector2 TilingAndOffset(Vector2 uv, Vector2 tiling, Vector2 offset) {
            return uv * tiling + offset;
        }
        
        private static float GetRipples(float2 uv, float t, float multiplier, float max_frequency, float speed) 
        {
            float l = length(uv);
            return sin(t * speed - l * l * max_frequency) * multiplier;
        }

        private static float2x2 Get2DRotationMatrix(float angle)
        {
            return float2x2(cos(angle), sin(angle), -sin(angle), cos(angle));
        }

        private static float GetOrganicFractal(float2 uv, float t, float scale, float scale_multiplication_step, 
            float rotation_step, float iterations, float ripples, float brightness) 
        {
            // Remap to [-1.0, 1.0].
            uv = float2(uv - 0.5f) * 2.0f;

            var n = float2.zero;
            float output = 0f;

            var rotation_matrix = Get2DRotationMatrix(rotation_step);

            for (int i = 0; i < iterations; i++)
            {
                uv = mul(rotation_matrix, uv);
                n = mul(rotation_matrix, n);

                var animated_uv = uv * scale + t;
                var q = animated_uv + ripples + i + n;

                output += dot(cos(q) / scale, float2(1f, 1f) * brightness);

                n -= sin(q);

                scale *= scale_multiplication_step;
            }

            return output;
        }
        
#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _showVertices;
        [SerializeField] private bool _showTestPoint;
        [SerializeField] private bool _showSamples;
        [SerializeField] private Vector3 _testPoint;
        
        private readonly List<Vector3> _vertices = new();
        
        private void OnDrawGizmos() {
            if (_showVertices) DrawVertices();
            if (_showTestPoint && _meshRenderer != null) DrawPoint(_meshRenderer.transform.TransformPoint(_testPoint));
        }
        
        private void DrawVertices() {
            if (_meshRenderer == null || _meshFilter == null) return;

            var trf = _meshRenderer.transform;
            var center  = _meshRenderer.localBounds.center;
            
            _meshFilter.sharedMesh.GetVertices(_vertices);
            
            for (int i = 0; i < _vertices.Count; i++) {
                var point = Sample(_vertices[i]);
                
                DebugExt.DrawRay(trf.TransformPoint(center + point), trf.up * 0.005f, Color.yellow, gizmo: true);
            }
        }

        private void DrawPoint(Vector3 point) {
            if (_meshRenderer == null) return;

            var trf = _meshRenderer.transform;
            var sample = point;
            
            if (Application.isPlaying) {
                if (!TrySamplePosition(ref sample)) return;
            }
            else {
                var center  = _meshRenderer.localBounds.center;
                sample = trf.TransformPoint(Sample(trf.InverseTransformPoint(point) - center) + center);
            }
            
            DebugExt.DrawCircle(sample, trf.rotation, 0.05f, Color.green, gizmo: true);
            DebugExt.DrawRay(sample, trf.up * 0.005f, Color.green, gizmo: true);
        }
#endif
    }
    
}