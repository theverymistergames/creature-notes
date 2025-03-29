using System.Collections.Generic;
using MisterGames.Common;
using MisterGames.Common.Maths;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using float2 = Unity.Mathematics.float2;

namespace _Project.Scripts.Runtime.Flesh {
    
    public sealed class FleshVertexPosition : MonoBehaviour {
        
        [SerializeField] private MeshFilter _meshFilter;
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] [Min(1)] private int _gridSizeX = 10;
        [SerializeField] [Min(1)] private int _gridSizeZ = 10;
        [SerializeField] private float _fractalMul = 1f;
        [SerializeField] private float _alphaPowR = 2.15f;
        [SerializeField] private float _timeOffset = 0f;
        
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

        private Vector3 Sample(Vector2 uv) {
#if UNITY_EDITOR
            var mat = Application.isPlaying ? _meshRenderer.material : _meshRenderer.sharedMaterial;
#else
            var mat = _meshRenderer.material;
#endif

            float time = Shader.GetGlobalVector(Time1).y + _timeOffset;
            
            float ripplesStrength = mat.GetFloat(RipplesStrength);
            float ripplesMaxFrequency = mat.GetFloat(RipplesMaxFrequency);
            float ripplesSpeed = mat.GetFloat(RipplesSpeed);
            
            Vector2 fractalTiling = mat.GetVector(FractalTiling);
            Vector2 fractalOffset = mat.GetVector(FractalOffset);
            
            Vector2 alphaTiling = mat.GetVector(AlphaTiling);
            Vector2 alphaOffset = mat.GetVector(AlphaOffset);
           
            Vector3 intensityVector = mat.GetVector(IntensityVector);
            Vector3 positionOffset = mat.GetVector(PositionOffset);
            
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
            
            float ripples = GetRipples(uv, time, ripplesStrength, ripplesMaxFrequency, ripplesSpeed);

            var fractalUv = TilingAndOffset(uv, fractalTiling, fractalOffset);
            float t = time * uvAnimationSpeed ;
            float fractal = GetOrganicFractal(fractalUv, t, fractalScale, fractalScaleStep, rotationStep, fractalIterations, ripples, fractalBrightness) * fractalStrength;
            float fractalMask = GetOrganicFractal(fractalUv, t, maskScale, maskScaleStep, rotationStep, maskIterations, ripples, maskBrightness) * maskStrength;

            float fractalInput = _fractalMul * (fractal * displacement + fractalMask * maskDisplacement);

            var alphaMaskColor = SampleTexture2D(alphaMask, TilingAndOffset(uv, alphaTiling, alphaOffset));
            
            var position = new Vector3(uv.x, 0f, uv.y);
            var normal = Vector3.up;

            var lerp = new Vector3(
                Mathf.Lerp(position.x, fractalInput * normal.x, alphaMaskColor.r * intensityVector.x),
                Mathf.Lerp(position.y, fractalInput * normal.y, alphaMaskColor.r * intensityVector.y),
                Mathf.Lerp(position.z, fractalInput * normal.z, alphaMaskColor.r * intensityVector.z)
            );

            return lerp + positionOffset * pow(alphaMaskColor.r, _alphaPowR);
        }
        
        private Vector3 CellToLocalPosition(int cell) {
            float x = (Mathf.FloorToInt((float) cell / _gridSizeZ) + 0.5f) / _gridSizeX - 0.5f;
            float z = (cell % _gridSizeZ + 0.5f) / _gridSizeZ - 0.5f;
            
            var size = _meshRenderer.localBounds.size;
            return new Vector3(size.x * x, 0f, size.z * z);
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
        [SerializeField] private bool _showGrid;
        [SerializeField] private bool _showVertices;

        private readonly List<Vector3> _vertices = new();
        
        private void OnDrawGizmos() {
            if (_showGrid) DrawGrid();
            if (_showVertices) DrawCellPositions();
        }

        private void DrawGrid() {
            if (_meshRenderer == null) return;

            var bounds = _meshRenderer.localBounds;
            var center  = bounds.center;
            var ext = bounds.extents.WithY(0f);
            var trf = _meshRenderer.transform;
            
            for (int i = 0; i <= _gridSizeX; i++) {
                float x = ((float) i / _gridSizeX - 0.5f) * ext.x * 2f;
                float z0 = ext.z;
                float z1 = -ext.z;
                
                var p0 = trf.TransformPoint(center + new Vector3(x, 0, z0));
                var p1 = trf.TransformPoint(center + new Vector3(x, 0, z1));
                
                DebugExt.DrawLine(p0, p1, Color.yellow, gizmo: true);
            }
            
            for (int i = 0; i <= _gridSizeZ; i++) {
                float z = ((float) i / _gridSizeZ - 0.5f) * ext.z * 2f;
                float x0 = ext.x;
                float x1 = -ext.x;
                
                var p0 = trf.TransformPoint(center + new Vector3(x0, 0, z));
                var p1 = trf.TransformPoint(center + new Vector3(x1, 0, z));
                
                DebugExt.DrawLine(p0, p1, Color.yellow, gizmo: true);
            }
        }
        
        private void DrawCellPositions() {
            if (_meshRenderer == null) return;

            var trf = _meshRenderer.transform;
            var bounds = _meshRenderer.localBounds;
            var center  = bounds.center;

            _meshFilter.sharedMesh.GetVertices(_vertices);
            
            for (int i = 0; i < _vertices.Count; i++) {
                var cellLocal = _vertices[i];
                var uv = new Vector2(cellLocal.x, cellLocal.z);
                var point = Sample(uv);
                
                DebugExt.DrawRay(trf.TransformPoint(center + point), trf.up * 0.005f, Color.yellow, gizmo: true);
            }
        }
#endif
    }
    
}