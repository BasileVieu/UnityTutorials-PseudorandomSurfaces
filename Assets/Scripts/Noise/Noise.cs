using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static partial class Noise
{
    public interface INoise
    {
        Sample4 GetNoise4(float4x3 positions, SmallXXHash4 hash, int frequency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Sample4 GetFractalNoise<N>(float4x3 position, Settings settings) where N : struct, INoise
    {
        SmallXXHash4 hash = SmallXXHash4.Seed(settings.seed);

        int frequency = settings.frequency;

        float amplitude = 1.0f;
        float amplitudeSum = 0.0f;

        Sample4 sum = default;

        for (int o = 0; o < settings.octaves; o++)
        {
            sum += amplitude * default(N).GetNoise4(position, hash + o, frequency);
            amplitudeSum += amplitude;
            frequency *= settings.lacunarity;
            amplitude *= settings.persistence;
        }

        return sum / amplitudeSum;
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct Job<N> : IJobFor where N : struct, INoise
    {
        [ReadOnly] private NativeArray<float3x4> m_positions;

        [WriteOnly] private NativeArray<float4> m_noise;

        private Settings m_settings;

        private float3x4 m_domainTRS;

        public void Execute(int i) =>
            m_noise[i] = GetFractalNoise<N>(m_domainTRS.TransformVectors(transpose(m_positions[i])), m_settings).v;

        public static JobHandle ScheduleParallel(NativeArray<float3x4> positions, NativeArray<float4> noise,
            Settings settings, SpaceTRS trs, int resolution, JobHandle dependency) =>
            new Job<N>
            {
                m_positions = positions,
                m_noise = noise,
                m_settings = settings,
                m_domainTRS = trs.Matrix
            }.ScheduleParallel(positions.Length, resolution, dependency);
    }

    public delegate JobHandle ScheduleDelegate(NativeArray<float3x4> positions, NativeArray<float4> noise,
        Settings settings, SpaceTRS trs, int resolution, JobHandle dependency);
}