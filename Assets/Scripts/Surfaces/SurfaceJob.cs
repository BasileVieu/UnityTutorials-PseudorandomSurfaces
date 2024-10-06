using ProceduralMeshes.Streams;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using static Noise;

[BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
public struct SurfaceJob<N> : IJobFor where N : struct, INoise
{
    private struct Vertex4
    {
        public SingleStream.Stream0 v0;
        public SingleStream.Stream0 v1;
        public SingleStream.Stream0 v2;
        public SingleStream.Stream0 v3;
    }

    private NativeArray<Vertex4> m_vertices;

    private Settings m_settings;

    private float3x4 m_domainTRS;

    private float3x3 m_derivativeMatrix;

    private float m_displacement;

    private bool m_isPlane;

    public void Execute(int i)
    {
        Vertex4 v = m_vertices[i];

        Sample4 noise = GetFractalNoise<N>(m_domainTRS.TransformVectors(transpose(float3x4(v.v0.position,
            v.v1.position, v.v2.position, v.v3.position))), m_settings) * m_displacement;

        noise.Derivatives = m_derivativeMatrix.TransformVectors(noise.Derivatives);

        if (m_isPlane)
        {
            m_vertices[i] = SetPlaneVertices(v, noise);
        }
        else
        {
            m_vertices[i] = SetSphereVertices(v, noise);
        }
    }

    Vertex4 SetPlaneVertices(Vertex4 v, Sample4 noise)
    {
        v.v0.position.y = noise.v.x;
        v.v1.position.y = noise.v.y;
        v.v2.position.y = noise.v.z;
        v.v3.position.y = noise.v.w;

        float4 normalizer = rsqrt(noise.dx * noise.dx + 1.0f);
        float4 tangentY = noise.dx * normalizer;

        v.v0.tangent = float4(normalizer.x, tangentY.x, 0.0f, -1.0f);
        v.v1.tangent = float4(normalizer.y, tangentY.y, 0.0f, -1.0f);
        v.v2.tangent = float4(normalizer.z, tangentY.z, 0.0f, -1.0f);
        v.v3.tangent = float4(normalizer.w, tangentY.w, 0.0f, -1.0f);

        normalizer = rsqrt(noise.dx * noise.dx + noise.dz * noise.dz + 1.0f);

        float4 normalX = -noise.dx * normalizer;
        float4 normalZ = -noise.dz * normalizer;

        v.v0.normal = float3(normalX.x, normalizer.x, normalZ.x);
        v.v1.normal = float3(normalX.y, normalizer.y, normalZ.y);
        v.v2.normal = float3(normalX.z, normalizer.z, normalZ.z);
        v.v3.normal = float3(normalX.w, normalizer.w, normalZ.w);

        return v;
    }

    Vertex4 SetSphereVertices(Vertex4 v, Sample4 noise)
    {
        noise.v += 1.0f;
        noise.dx /= noise.v;
        noise.dy /= noise.v;
        noise.dz /= noise.v;

        float4x3 p = transpose(float3x4(v.v0.position, v.v1.position, v.v2.position, v.v3.position));

        float3 tangentCheck = abs(v.v0.tangent.xyz);

        if (tangentCheck.x + tangentCheck.y + tangentCheck.z > 0.0f)
        {
            float4x3 t = transpose(float3x4(v.v0.tangent.xyz, v.v1.tangent.xyz, v.v2.tangent.xyz, v.v3.tangent.xyz));

            float4 td = t.c0 * noise.dx + t.c1 * noise.dy + t.c2 * noise.dz;

            t.c0 += td * p.c0;
            t.c1 += td * p.c1;
            t.c2 += td * p.c2;

            float3x4 tt = transpose(t.NormalizeRows());

            v.v0.tangent = float4(tt.c0, -1.0f);
            v.v1.tangent = float4(tt.c1, -1.0f);
            v.v2.tangent = float4(tt.c2, -1.0f);
            v.v3.tangent = float4(tt.c3, -1.0f);
        }

        float4 pd = p.c0 * noise.dx + p.c1 * noise.dy + p.c2 * noise.dz;

        float3x4 nt = transpose(float4x3(p.c0 - noise.dx + pd * p.c0,
            p.c1 - noise.dy + pd * p.c1,
            p.c2 - noise.dz + pd * p.c2).NormalizeRows());

        v.v0.normal = nt.c0;
        v.v1.normal = nt.c1;
        v.v2.normal = nt.c2;
        v.v3.normal = nt.c3;

        v.v0.position *= noise.v.x;
        v.v1.position *= noise.v.y;
        v.v2.position *= noise.v.z;
        v.v3.position *= noise.v.w;

        return v;
    }

    public static JobHandle ScheduleParallel(Mesh.MeshData meshData, int resolution,
        Settings settings, SpaceTRS domain, float displacement, bool isPlane, JobHandle dependency) =>
        new SurfaceJob<N>
        {
            m_vertices = meshData.GetVertexData<SingleStream.Stream0>().Reinterpret<Vertex4>(12 * 4),
            m_settings = settings,
            m_domainTRS = domain.Matrix,
            m_derivativeMatrix = domain.DerivativeMatrix,
            m_displacement = displacement,
            m_isPlane = isPlane
        }.ScheduleParallel(meshData.vertexCount / 4, resolution, dependency);
}

public delegate JobHandle SurfaceJobScheduleDelegate(Mesh.MeshData meshData, int resolution, Settings settings,
    SpaceTRS domain, float displacement, bool isPlane, JobHandle dependency);