using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralMeshes.Streams
{
    public struct MultiStream : IMeshStreams
    {
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float3> stream0;
        
        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float3> stream1;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float4> stream2;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<float2> stream3;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<TriangleUInt16> triangles;

        public void Setup(Mesh.MeshData _meshData, Bounds _bounds, int _vertexCount, int _indexCount)
        {
            var descriptor = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
            descriptor[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3, stream: 1);
            descriptor[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, dimension: 4, stream: 2);
            descriptor[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2, stream: 3);
            
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
            stream1 = _meshData.GetVertexData<float3>(1);
            stream2 = _meshData.GetVertexData<float4>(2);
            stream3 = _meshData.GetVertexData<float2>(3);
            
            triangles = _meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int _index, Vertex _vertex)
        {
            stream0[_index] = _vertex.position;
            stream1[_index] = _vertex.normal;
            stream2[_index] = _vertex.tangent;
            stream3[_index] = _vertex.texCoord0;
        }

        public void SetTriangle(int _index, int3 _triangle) => triangles[_index] = _triangle;
    }
}