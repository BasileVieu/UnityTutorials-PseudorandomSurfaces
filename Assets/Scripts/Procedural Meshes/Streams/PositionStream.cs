using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralMeshes.Streams
{
    public struct PositionStream : IMeshStreams
    {
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float3> stream0;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<TriangleUInt16> triangles;

        public void Setup(Mesh.MeshData _meshData, Bounds _bounds, int _vertexCount, int _indexCount)
        {
            var descriptor = new NativeArray<VertexAttributeDescriptor>(1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
            
            _meshData.SetVertexBufferParams(_vertexCount, descriptor);
            
            descriptor.Dispose();

            _meshData.SetIndexBufferParams(_indexCount, IndexFormat.UInt16);

            _meshData.subMeshCount = 1;
            
            _meshData.SetSubMesh(0, new SubMeshDescriptor(0, _indexCount)
                {
                    bounds = _bounds,
                    vertexCount = _vertexCount
                },
                MeshUpdateFlags.DontRecalculateBounds |
                MeshUpdateFlags.DontValidateIndices
            );

            stream0 = _meshData.GetVertexData<float3>();
            triangles = _meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int _index, Vertex _vertex)
        {
            stream0[_index] = _vertex.position;
        }

        public void SetTriangle(int _index, int3 _triangle) => triangles[_index] = _triangle;
    }
}