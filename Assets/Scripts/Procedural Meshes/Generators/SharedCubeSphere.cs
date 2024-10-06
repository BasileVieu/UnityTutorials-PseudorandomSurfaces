using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace ProceduralMeshes.Generators
{
    public struct SharedCubeSphere : IMeshGenerator
    {
        private struct Side
        {
            public int id;
            
            public float3 uvOrigin;
            public float3 uVector;
            public float3 vVector;
            
            public int seamStep;

            public bool TouchesMinimumPole => (id & 1) == 0;
        }

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));

        public int VertexCount => 6 * Resolution * Resolution + 2;

        public int IndexCount => 6 * 6 * Resolution * Resolution;

        public int JobLength => 6 * Resolution;

        public int Resolution { get; set; }

        public void Execute<S>(int _i, S _streams) where S : struct, IMeshStreams
        {
            int u = _i / 6;
            
            Side side = GetSide(_i - 6 * u);
            
            int vi = Resolution * (Resolution * side.id + u) + 2;
            int ti = 2 * Resolution * (Resolution * side.id + u);
            
            bool firstColumn = u == 0;
            
            u += 1;

            float3 pStart = side.uvOrigin + side.uVector * u / Resolution;

            Vertex vertex = new Vertex();
            
            if (_i == 0)
            {
                vertex.position = -sqrt(1f / 3f);
                
                _streams.SetVertex(0, vertex);
                
                vertex.position = sqrt(1f / 3f);
                
                _streams.SetVertex(1, vertex);
            }

            vertex.position = CubeToSphere(pStart);
            
            _streams.SetVertex(vi, vertex);

            int3 triangle = int3(vi,
                firstColumn && side.TouchesMinimumPole ? 0 : vi - Resolution,
                vi + (firstColumn ? side.TouchesMinimumPole ? side.seamStep * Resolution * Resolution : Resolution == 1 ? side.seamStep : -Resolution + 1
                    : -Resolution + 1));

            _streams.SetTriangle(ti, triangle);
            
            vi += 1;
            ti += 1;

            int zAdd = firstColumn && side.TouchesMinimumPole ? Resolution : 1;
            int zAddLast = firstColumn && side.TouchesMinimumPole ? Resolution :
                !firstColumn && !side.TouchesMinimumPole ? Resolution * ((side.seamStep + 1) * Resolution - u) + u :
                (side.seamStep + 1) * Resolution * Resolution - Resolution + 1;

            for (int v = 1; v < Resolution; v++, vi++, ti += 2)
            {
                vertex.position = CubeToSphere(pStart + side.vVector * v / Resolution);
                
                _streams.SetVertex(vi, vertex);

                triangle.x += 1;
                triangle.y = triangle.z;
                triangle.z += v == Resolution - 1 ? zAddLast : zAdd;
                
                _streams.SetTriangle(ti + 0, int3(triangle.x - 1, triangle.y, triangle.x));
                _streams.SetTriangle(ti + 1, triangle);
            }

            _streams.SetTriangle(ti, int3(triangle.x,
                triangle.z,
                side.TouchesMinimumPole ? triangle.z + Resolution : u == Resolution ? 1 : triangle.z + 1));
        }

        private static float3 CubeToSphere(float3 _p) => _p * sqrt(1f - ((_p * _p).yxx + (_p * _p).zzy) / 2f + (_p * _p).yxx * (_p * _p).zzy / 3f);

        private static Side GetSide(int _id) => _id switch
        {
            0 => new Side
            {
                id = _id,
                uvOrigin = -1f,
                uVector = 2f * right(),
                vVector = 2f * up(),
                seamStep = 4
            },
            1 => new Side
            {
                id = _id,
                uvOrigin = float3(1f, -1f, -1f),
                uVector = 2f * forward(),
                vVector = 2f * up(),
                seamStep = 4
            },
            2 => new Side
            {
                id = _id,
                uvOrigin = -1f,
                uVector = 2f * forward(),
                vVector = 2f * right(),
                seamStep = -2
            },
            3 => new Side
            {
                id = _id,
                uvOrigin = float3(-1f, -1f, 1f),
                uVector = 2f * up(),
                vVector = 2f * right(),
                seamStep = -2
            },
            4 => new Side
            {
                id = _id,
                uvOrigin = -1f,
                uVector = 2f * up(),
                vVector = 2f * forward(),
                seamStep = -2
            },
            _ => new Side
            {
                id = _id,
                uvOrigin = float3(-1f, 1f, -1f),
                uVector = 2f * right(),
                vVector = 2f * forward(),
                seamStep = -2
            }
        };
    }
}