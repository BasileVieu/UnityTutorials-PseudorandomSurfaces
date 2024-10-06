using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace ProceduralMeshes
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct MeshJob<G, S> : IJobFor
        where G : struct, IMeshGenerator
        where S : struct, IMeshStreams
    {
        private G generator;

        [WriteOnly] private S streams;

        public void Execute(int _i) => generator.Execute(_i, streams);

        public static JobHandle ScheduleParallel(Mesh _mesh, Mesh.MeshData _meshData, int _resolution,
            JobHandle _dependency) => ScheduleParallel(_mesh, _meshData, _resolution, _dependency, Vector3.zero,
            false);

        public static JobHandle ScheduleParallel(Mesh _mesh, Mesh.MeshData _meshData, int _resolution,
            JobHandle _dependency, Vector3 _extraBoundsExtents, bool _supportVectorization)
        {
            var job = new MeshJob<G, S>();
            job.generator.Resolution = _resolution;

            int vertexCount = job.generator.VertexCount;

            if (_supportVectorization
                && (vertexCount & 0b11) != 0)
            {
                vertexCount += 4 - (vertexCount & 0b11);
            }

            Bounds bounds = job.generator.Bounds;
            bounds.extents += _extraBoundsExtents;

            job.streams.Setup(_meshData,
                _mesh.bounds = bounds,
                vertexCount,
                job.generator.IndexCount);

            return job.ScheduleParallel(job.generator.JobLength, 1, _dependency);
        }
    }

    public delegate JobHandle MeshJobScheduleDelegate(Mesh _mesh, Mesh.MeshData _meshData, int _resolution,
        JobHandle _dependency);

    public delegate JobHandle AdvancedMeshJobScheduleDelegate(Mesh _mesh, Mesh.MeshData _meshData, int _resolution,
        JobHandle _dependency, Vector3 _extraBoundsExtents, bool _supportVectorization);
}