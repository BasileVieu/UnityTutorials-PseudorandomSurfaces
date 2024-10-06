using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using quaternion = Unity.Mathematics.quaternion;

namespace ProceduralMeshes.Generators
{
    public struct GeoIcosphere : IMeshGenerator
    {
        private struct Strip
        {
            public int id;
            public float3 lowLeftCorner;
            public float3 lowRightCorner;
            public float3 highLeftCorner;
            public float3 highRightCorner;

            public float3 bottomLeftAxis;
            public float3 bottomRightAxis;
            public float3 midLeftAxis;
            public float3 midCenterAxis;
            public float3 midRightAxis;
            public float3 topLeftAxis;
            public float3 topRightAxis;
        }

        public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));

        public int VertexCount => 5 * ResolutionV * Resolution + 2;

        public int IndexCount => 6 * 5 * ResolutionV * Resolution;

        public int JobLength => 5 * Resolution;

        public int Resolution { get; set; }

        private int ResolutionV => 2 * Resolution;

        public void Execute<S>(int _i, S _streams) where S : struct, IMeshStreams
        {
            int u = _i / 5;
            
            Strip strip = GetStrip(_i - 5 * u);
            
            int vi = ResolutionV * (Resolution * strip.id + u) + 2;
            int ti = 2 * ResolutionV * (Resolution * strip.id + u);
            
            bool firstColumn = u == 0;

            int4 quad = int4(vi,
                firstColumn ? 0 : vi - ResolutionV,
                firstColumn ? strip.id == 0 ? 4 * ResolutionV * Resolution + 2 : vi - ResolutionV * (Resolution + u) : vi - ResolutionV + 1,
                vi + 1);

            u += 1;

            Vertex vertex = new Vertex();
            
            if (_i == 0)
            {
                vertex.position = down();
                _streams.SetVertex(0, vertex);
                vertex.position = up();
                _streams.SetVertex(1, vertex);
            }

            vertex.position = mul(quaternion.AxisAngle(strip.bottomRightAxis, EdgeRotationAngle * u / Resolution), down());
            
            _streams.SetVertex(vi, vertex);
            
            vi += 1;

            for (int v = 1; v < ResolutionV; v++, vi++, ti += 2)
            {
                float h = u + v;
                
                float3 leftAxis;
                float3 rightAxis;
                float3 leftStart;
                float3 rightStart;

                float edgeAngleScale;
                float faceAngleScale;
                
                if (v <= Resolution - u)
                {
                    leftAxis = strip.bottomLeftAxis;
                    rightAxis = strip.bottomRightAxis;
                    leftStart = rightStart = down();
                    edgeAngleScale = h / Resolution;
                    faceAngleScale = v / h;
                }
                else if (v < Resolution)
                {
                    leftAxis = strip.midCenterAxis;
                    rightAxis = strip.midRightAxis;
                    leftStart = strip.lowLeftCorner;
                    rightStart = strip.lowRightCorner;
                    edgeAngleScale = h / Resolution - 1f;
                    faceAngleScale = (Resolution - u) / (ResolutionV - h);
                }
                else if (v <= ResolutionV - u)
                {
                    leftAxis = strip.midLeftAxis;
                    rightAxis = strip.midCenterAxis;
                    leftStart = rightStart = strip.lowLeftCorner;
                    edgeAngleScale = h / Resolution - 1f;
                    faceAngleScale = (v - Resolution) / (h - Resolution);
                }
                else
                {
                    leftAxis = strip.topLeftAxis;
                    rightAxis = strip.topRightAxis;
                    leftStart = strip.highLeftCorner;
                    rightStart = strip.highRightCorner;
                    edgeAngleScale = h / Resolution - 2f;
                    faceAngleScale = (Resolution - u) / (3f * Resolution - h);
                }

                float3 pLeft = mul(quaternion.AxisAngle(leftAxis, EdgeRotationAngle * edgeAngleScale), leftStart);
                float3 pRight = mul(quaternion.AxisAngle(rightAxis, EdgeRotationAngle * edgeAngleScale), rightStart);
                float3 axis = normalize(cross(pRight, pLeft));
                
                float angle = acos(dot(pRight, pLeft)) * faceAngleScale;
                
                vertex.position = mul(quaternion.AxisAngle(axis, angle), pRight);
                
                _streams.SetVertex(vi, vertex);
                
                _streams.SetTriangle(ti + 0, quad.xyz);
                _streams.SetTriangle(ti + 1, quad.xzw);

                quad.y = quad.z;
                quad += int4(1, 0, firstColumn && v <= Resolution - u ? ResolutionV : 1, 1);
            }

            if (!firstColumn)
            {
                quad.z = ResolutionV * Resolution * (strip.id == 0 ? 5 : strip.id) - Resolution + u + 1;
            }

            quad.w = u < Resolution ? quad.z + 1 : 1;

            _streams.SetTriangle(ti + 0, quad.xyz);
            _streams.SetTriangle(ti + 1, quad.xzw);
        }

        private static Strip GetStrip(int _id) => _id switch
        {
            0 => CreateStrip(0),
            1 => CreateStrip(1),
            2 => CreateStrip(2),
            3 => CreateStrip(3),
            _ => CreateStrip(4)
        };

        private static Strip CreateStrip(int _id)
        {
            Strip s = new Strip
            {
                id = _id,
                lowLeftCorner = GetCorner(2 * _id, -1),
                lowRightCorner = GetCorner(_id == 4 ? 0 : 2 * _id + 2, -1),
                highLeftCorner = GetCorner(_id == 0 ? 9 : 2 * _id - 1, 1),
                highRightCorner = GetCorner(2 * _id + 1, 1)
            };
            
            s.bottomLeftAxis = normalize(cross(down(), s.lowLeftCorner));
            s.bottomRightAxis = normalize(cross(down(), s.lowRightCorner));
            s.midLeftAxis = normalize(cross(s.lowLeftCorner, s.highLeftCorner));
            s.midCenterAxis = normalize(cross(s.lowLeftCorner, s.highRightCorner));
            s.midRightAxis = normalize(cross(s.lowRightCorner, s.highRightCorner));
            s.topLeftAxis = normalize(cross(s.highLeftCorner, up()));
            s.topRightAxis = normalize(cross(s.highRightCorner, up()));
            
            return s;
        }

        private static float3 GetCorner(int _id, int _ySign) => float3(0.4f * sqrt(5f) * sin(0.2f * PI * _id),
            _ySign * 0.2f * sqrt(5f),
            -0.4f * sqrt(5f) * cos(0.2f * PI * _id));

        private static float EdgeRotationAngle => acos(dot(up(), GetCorner(0, 1)));
    }
}