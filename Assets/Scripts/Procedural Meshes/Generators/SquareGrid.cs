using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace ProceduralMeshes.Generators
{
    public struct SquareGrid : IMeshGenerator
    {
        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1f, 0f, 1f));

        public int VertexCount => 4 * Resolution * Resolution;

        public int IndexCount => 6 * Resolution * Resolution;

        public int JobLength => Resolution;

        public int Resolution { get; set; }

        public void Execute<S>(int _z, S _streams) where S : struct, IMeshStreams
        {
            int vi = 4 * Resolution * _z, ti = 2 * Resolution * _z;

            for (int x = 0; x < Resolution; x++, vi += 4, ti += 2)
            {
                float2 xCoordinates = float2(x, x + 1f) / Resolution - 0.5f;
                float2 zCoordinates = float2(_z, _z + 1f) / Resolution - 0.5f;

                Vertex vertex = new Vertex();
                vertex.normal.y = 1f;
                vertex.tangent.xw = float2(1f, -1f);

                vertex.position.x = xCoordinates.x;
                vertex.position.z = zCoordinates.x;
                
                _streams.SetVertex(vi + 0, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.texCoord0 = float2(1f, 0f);
                
                _streams.SetVertex(vi + 1, vertex);

                vertex.position.x = xCoordinates.x;
                vertex.position.z = zCoordinates.y;
                vertex.texCoord0 = float2(0f, 1f);
                
                _streams.SetVertex(vi + 2, vertex);

                vertex.position.x = xCoordinates.y;
                vertex.texCoord0 = 1f;
                
                _streams.SetVertex(vi + 3, vertex);

                _streams.SetTriangle(ti + 0, vi + int3(0, 2, 1));
                _streams.SetTriangle(ti + 1, vi + int3(1, 2, 3));
            }
        }
    }
}