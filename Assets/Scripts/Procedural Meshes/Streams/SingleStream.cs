using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralMeshes.Streams
{
    public struct SingleStream : IMeshStreams
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Stream0
        {
            public float3 position;
            public float3 normal;
            public float4 tangent;
            public float2 texCoord0;
        }

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<Stream0> stream0;

        [NativeDisableContainerSafetyRestriction]
        private NativeArray<TriangleUInt16> triangles;

        public void Setup(Mesh.MeshData _meshData, Bounds _bounds, int _vertexCount, int _indexCount)
        {
            var descriptor = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
            descriptor[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3);
            descriptor[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, dimension: 4);
            descriptor[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2);
            
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

            stream0 = _meshData.GetVertexData<Stream0>();
            triangles = _meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertex(int _index, Vertex _vertex) => stream0[_index] = new Stream0
        {
            position = _vertex.position,
            normal = _vertex.normal,
            tangent = _vertex.tangent,
            texCoord0 = _vertex.texCoord0
        };

        public void SetTriangle(int _index, int3 _triangle) => triangles[_index] = _triangle;
    }
}