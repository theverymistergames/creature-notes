using UnityEngine;
using System.Collections.Generic;
using MisterGames.Common;

[ExecuteAlways]
public class ParticlePlexus : MonoBehaviour
{
    // Particle system and particles array/list.

    ParticleSystem particleSystem;
    ParticleSystem.Particle[] particles;

    // Line distance threshold.

    public float radius = 1.0f;

    [Space]

    // Maximum number of lines to render.

    [Range(0, 8192)]
    public int maxLineRenderers = 1000;

    [Range(0.0f, 1.0f)]
    public float lineWidth = 0.2f;

    [Space]

    // Template for lines connecting particles.

    public LineRenderer lineRendererPrefab;

    // Line renderer pool.

    LineRenderer[] lineRenderers;

    // Triangles.

    Mesh mesh; // Generated triangles mesh.
    public MeshFilter meshFilter;

    // List of triangle indices.

    List<int> meshTriangles = new();

    // Mesh data arrays.

    Vector3[] meshVertices;
    Color[] meshColours;
    Vector2[] meshUVs;

    [Space]

    public bool debugDrawLines = false;

    // Render a line between pA and pB,
    // with start/end colours cA and cB.

    // n = number of segments to render.

    static void DebugDrawLineGradient(
        Vector3 pA, Vector3 pB, Color cA, Color cB, uint n)
    {
        for (uint i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float tNext = (i + 1.0f) / n;

            Vector3 a = Vector3.Lerp(pA, pB, t);
            Vector3 b = Vector3.Lerp(pA, pB, tNext);

            Color c = Color.Lerp(cA, cB, t);

            Debug.DrawLine(a, b, c);
        }
    }

    void DestroyAllLineRenderersIfNotNull()
    {
        if (lineRenderers != null)
        {
            for (int i = 0; i < lineRenderers.Length; i++)
            {
                if (lineRenderers[i] != null)
                {
                    DestroyImmediate(lineRenderers[i].gameObject);
                }
            }
        }
    }

    void OnDestroy()
    {
        DestroyAllLineRenderersIfNotNull();
    }

    void LateUpdate()
    {
        // Get particle system component if null.

        if (particleSystem == null)
        {
            particleSystem = GetComponent<ParticleSystem>();
        }

        // Initialize particles array if null or size mismatch to max.

        int maxParticleCount = particleSystem.main.maxParticles;

        if (particles == null || particles.Length != maxParticleCount)
        {
            particles = new ParticleSystem.Particle[maxParticleCount];
        }

        // Load particles from system into our array.

        int particleCount = particleSystem.GetParticles(particles);

        // Create and assign mesh if null.

        if (mesh == null)
        {
            mesh = new Mesh();
            meshFilter.mesh = mesh;
        }

        // Clear mesh data.

        meshTriangles.Clear();

        // Create/resize mesh data arrays.

        if (meshVertices == null || meshVertices.Length != maxParticleCount)
        {
            meshVertices = new Vector3[maxParticleCount];
        }
        if (meshColours == null || meshColours.Length != maxParticleCount)
        {
            meshColours = new Color[maxParticleCount];
        }
        if (meshUVs == null || meshUVs.Length != maxParticleCount)
        {
            meshUVs = new Vector2[maxParticleCount];
        }

        // Compare each particle to every other particle.

        int lineRendererCount = 0;

        // Create line renderer pool.
        // Leaving this in Update allows for resizing maxLines at runtime.

        if (Application.isPlaying)
        {
            if (lineRenderers == null || lineRenderers.Length != maxLineRenderers)
            {
                // If line renderers already exist, destroy them.

                DestroyAllLineRenderersIfNotNull();

                // Create new line renderers.

                lineRenderers = new LineRenderer[maxLineRenderers];

                // Instantiate line renderers from prefab.
                // > this transform as parent.

                for (int i = 0; i < lineRenderers.Length; i++)
                {
                    lineRenderers[i] =
                        Instantiate(lineRendererPrefab, transform);
                }
            }
        }

        var meshVertexOffset = particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.World 
            ? -transform.position 
            : Vector3.zero;

        var lineRendererOffset = particleSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local
            ? transform.position
            : Vector3.zero;

        for (int i = 0; i < particleCount; i++)
        {
            meshVertices[i] = particles[i].position + meshVertexOffset;
            
            var particleA = particles[i];

            for (int j = i + 1; j < particleCount; j++)
            {
                var particleB = particles[j];

                // Distance: A -> B.
                // If distance between particles < threshold, we have a line/connection.

                if (Vector3.Distance(particleA.position, particleB.position) < radius)
                {
                    Color colourA = particleA.GetCurrentColor(particleSystem);
                    Color colourB = particleB.GetCurrentColor(particleSystem);

                    if (debugDrawLines)
                    {
                        DebugDrawLineGradient(
                            particleA.position, particleB.position, colourA, colourB, 8);
                    }

                    // Find triangles.

                    for (int k = j + 1; k < particleCount; k++)
                    {
                        var particleC = particles[k];

                        // Distance: A -> C and B -> C.
                        // If distance between particles < threshold, we have a triangle.

                        if (Vector3.Distance(particleA.position, particleC.position) < radius &&
                            Vector3.Distance(particleB.position, particleC.position) < radius)
                        {
                            // Triangle with vertices: Particle A, B, and C (indices- i, j, k).

                            meshTriangles.Add(i);
                            meshTriangles.Add(j);
                            meshTriangles.Add(k);

                            // UVs and colours.

                            meshUVs[i] = new Vector2(0.0f, 0.0f);
                            meshUVs[j] = new Vector2(0.0f, 1.0f);
                            meshUVs[k] = new Vector2(1.0f, 1.0f);

                            Color colourC = particleC.GetCurrentColor(particleSystem);

                            meshColours[i] = colourA;
                            meshColours[j] = colourB;
                            meshColours[k] = colourC;
                        }
                    }

                    if (Application.isPlaying)
                    {
                        if (lineRendererCount < lineRenderers.Length)
                        {
                            var line = lineRenderers[lineRendererCount];

                            line.SetPosition(0, particleA.position + lineRendererOffset);
                            line.SetPosition(1, particleB.position + lineRendererOffset);

                            line.startColor = colourA;
                            line.endColor = colourB;

                            float sizeA = particleA.GetCurrentSize(particleSystem);
                            float sizeB = particleB.GetCurrentSize(particleSystem);

                            line.startWidth = sizeA * lineWidth;
                            line.endWidth = sizeB * lineWidth;

                            line.gameObject.SetActive(true);

                            lineRendererCount++;
                        }
                    }
                }
            }
        }

        if (Application.isPlaying)
        {
            // Disable any remaining line renderers.

            for (int i = lineRendererCount; i < lineRenderers.Length; i++)
            {
                lineRenderers[i].gameObject.SetActive(false);
            }
        }

        // Set triangle mesh data.

        mesh.Clear(); // Required to avoid 'random' mesh errors.
        
        mesh.SetVertices(meshVertices, 0, particleCount);

        // Set triangles AFTER vertices are set, since indices reference vertices.

        mesh.SetTriangles(meshTriangles, 0);
        mesh.SetColors(meshColours, 0, particleCount);
        mesh.SetUVs(0, meshUVs, 0, particleCount);

        mesh.RecalculateNormals();
    }
}