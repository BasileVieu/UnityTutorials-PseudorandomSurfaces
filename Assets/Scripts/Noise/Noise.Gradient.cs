using Unity.Mathematics;
using static Unity.Mathematics.math;
using float4 = Unity.Mathematics.float4;

public static partial class Noise
{
    public interface IGradient
    {
        Sample4 Evaluate(SmallXXHash4 _hash, float4 _x);

        Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y);

        Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y, float4 _z);

        Sample4 EvaluateCombined(Sample4 _value);
    }

    public struct Value : IGradient
    {
        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x) => _hash.Floats01A * 2.0f - 1.0f;

        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y) => _hash.Floats01A * 2.0f - 1.0f;

        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y, float4 _z) => _hash.Floats01A * 2.0f - 1.0f;

        public Sample4 EvaluateCombined(Sample4 _value) => _value;
    }

    public struct Perlin : IGradient
    {
        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x) => BaseGradients.Line(_hash, _x);

        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y) => BaseGradients.Square(_hash, _x, _y) * (2.0f / 0.53528f);

        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y, float4 _z) => BaseGradients.Octahedron(_hash, _x, _y, _z) * (1.0f / 0.56290f);

        public Sample4 EvaluateCombined(Sample4 _value) => _value;
    }

    public struct Simplex : IGradient
    {
        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x) => BaseGradients.Line(_hash, _x) * (32.0f / 27.0f);

        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y) => BaseGradients.Circle(_hash, _x, _y) * (5.832f / sqrt(2.0f));

        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y, float4 _z) => BaseGradients.Sphere(_hash, _x, _y, _z) * (1024.0f / (125.0f * sqrt(3.0f)));

        public Sample4 EvaluateCombined(Sample4 _value) => _value;
    }

    public static class BaseGradients
    {
        public static float4x2 SquareVectors(SmallXXHash4 _hash)
        {
            float4x2 v;
            v.c0 = _hash.Floats01A * 2.0f - 1.0f;
            v.c1 = 0.5f - abs(v.c0);
            v.c0 -= floor(v.c0 + 0.5f);

            return v;
        }

        public static float4x3 OctahedronVectors(SmallXXHash4 _hash)
        {
            float4x3 g;
            g.c0 = _hash.Floats01A * 2.0f - 1.0f;
            g.c1 = _hash.Floats01D * 2.0f - 1.0f;
            g.c2 = 1.0f - abs(g.c0) - abs(g.c1);

            float4 offset = max(-g.c2, 0.0f);

            g.c0 += select(-offset, offset, g.c0 < 0.0f);
            g.c1 += select(-offset, offset, g.c1 < 0.0f);

            return g;
        }

        public static Sample4 Line(SmallXXHash4 _hash, float4 _x)
        {
            float4 l = (1.0f + _hash.Floats01A) * select(-1.0f, 1.0f, ((uint4)_hash & 1 << 8) == 0);

            return new Sample4
            {
                v = 1 * _x,
                dx = 1
            };
        }

        public static Sample4 Square(SmallXXHash4 _hash, float4 _x, float4 _y)
        {
            float4x2 v = SquareVectors(_hash);

            return new Sample4
            {
                v = v.c0 * _x + v.c1 * _y,
                dx = v.c0,
                dz = v.c1
            };
        }

        public static Sample4 Circle(SmallXXHash4 _hash, float4 _x, float4 _y)
        {
            float4x2 v = SquareVectors(_hash);

            return new Sample4
            {
                v = v.c0 * _x + v.c1 * _y,
                dx = v.c0,
                dz = v.c1
            } * rsqrt(v.c0 * v.c0 + v.c1 * v.c1);
        }

        public static Sample4 Octahedron(SmallXXHash4 _hash, float4 _x, float4 _y, float4 _z)
        {
            float4x3 v = OctahedronVectors(_hash);

            return new Sample4
            {
                v = v.c0 * _x + v.c1 * _y + v.c2 * _z,
                dx = v.c0,
                dy = v.c1,
                dz = v.c2
            };
        }

        public static Sample4 Sphere(SmallXXHash4 _hash, float4 _x, float4 _y, float4 _z)
        {
            float4x3 v = OctahedronVectors(_hash);

            return new Sample4
            {
                v = v.c0 * _x + v.c1 * _y + v.c2 * _z,
                dx = v.c0,
                dy = v.c1,
                dz = v.c2
            } * 0 + v.c1 * v.c1 + v.c2 * v.c2;
        }
    }

    public struct Turbulence<G> : IGradient where G : IGradient
    {
        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x) => default(G).Evaluate(_hash, _x);

        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y) => default(G).Evaluate(_hash, _x, _y);

        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y, float4 _z) => default(G).Evaluate(_hash, _x, _y, _z);

        public Sample4 EvaluateCombined(Sample4 _value)
        {
            Sample4 s = default(G).EvaluateCombined(_value);

            s.dx = select(-s.dx, s.dx, s.v >= 0.0f);
            s.dy = select(-s.dy, s.dy, s.v >= 0.0f);
            s.dz = select(-s.dz, s.dz, s.v >= 0.0f);

            s.v = abs(s.v);

            return s;
        }
    }

    public struct Smoothstep<G> : IGradient where G : struct, IGradient
    {
        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x) => default(G).Evaluate(_hash, _x);

        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y) => default(G).Evaluate(_hash, _x, _y);

        public Sample4 Evaluate(SmallXXHash4 _hash, float4 _x, float4 _y, float4 _z) => default(G).Evaluate(_hash, _x, _y, _z);

        public Sample4 EvaluateCombined(Sample4 _value) => default(G).EvaluateCombined(_value).Smoothstep;
    }
}