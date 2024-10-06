using Unity.Mathematics;

using static Unity.Mathematics.math;
using float4x3 = Unity.Mathematics.float4x3;

public static partial class Noise
{
    public struct Sample4
    {
        public float4 v;
        public float4 dx;
        public float4 dy;
        public float4 dz;

        public Sample4 Smoothstep
        {
            get
            {
                Sample4 s = this;

                float4 d = 6.0f * v * (1.0f - v);

                s.dx *= d;
                s.dy *= d;
                s.dz *= d;
                s.v *= v * (3.0f - 2.0f * v);

                return s;
            }
        }

        public static implicit operator Sample4(float4 _v) => new Sample4
        {
            v = _v
        };

        public static Sample4 operator +(Sample4 _a, Sample4 _b) => new Sample4
        {
            v = _a.v + _b.v,
            dx = _a.dx + _b.dx,
            dy = _a.dy + _b.dy,
            dz = _a.dz + _b.dz
        };

        public static Sample4 operator -(Sample4 _a, Sample4 _b) => new Sample4
        {
            v = _a.v - _b.v,
            dx = _a.dx - _b.dx,
            dy = _a.dy - _b.dy,
            dz = _a.dz - _b.dz
        };

        public static Sample4 operator *(Sample4 _a, float4 _b) => new Sample4
        {
            v = _a.v * _b,
            dx = _a.dx * _b,
            dy = _a.dy * _b,
            dz = _a.dz * _b
        };

        public static Sample4 operator *(float4 _a, Sample4 _b) => _b * _a;

        public static Sample4 operator /(Sample4 _a, float4 _b) => new Sample4
        {
            v = _a.v / _b,
            dx = _a.dx / _b,
            dy = _a.dy / _b,
            dz = _a.dz / _b
        };

        public float4x3 Derivatives
        {
            get => float4x3(dx, dy, dz);
            set
            {
                dx = value.c0;
                dy = value.c1;
                dz = value.c2;
            }
        }

        public static Sample4 Select(Sample4 _f, Sample4 _t, bool4 _b) => new Sample4
        {
            v = select(_f.v, _t.v, _b),
            dx = select(_f.dx, _t.dx, _b),
            dy = select(_f.dy, _t.dy, _b),
            dz = select(_f.dz, _t.dz, _b)
        };
    }
}