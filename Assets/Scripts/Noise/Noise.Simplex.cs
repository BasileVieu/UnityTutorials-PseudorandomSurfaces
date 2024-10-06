using Unity.Mathematics;
using static Unity.Mathematics.math;
using float4 = Unity.Mathematics.float4;

public static partial class Noise
{
    public struct Simplex1D<G> : INoise where G : struct, IGradient
    {
        public Sample4 GetNoise4(float4x3 _positions, SmallXXHash4 _hash, int _frequency)
        {
            _positions *= _frequency;

            int4 x0 = (int4)floor(_positions.c0);
            int4 x1 = x0 + 1;
            
            Sample4 s = default(G).EvaluateCombined(Kernel(_hash.Eat(x0), x0, _positions) + Kernel(_hash.Eat(x1), x1, _positions));

            s.dx *= _frequency;

            return s;
        }

        static Sample4 Kernel(SmallXXHash4 _hash, float4 _lx, float4x3 _positions)
        {
            float4 x = _positions.c0 - _lx;
            float4 f = 1.0f - x * x;

            Sample4 g = default(G).Evaluate(_hash, x);

            return new Sample4
            {
                v = f * g.v,
                dx = f * g.dx - 6.0f * x * g.v
            } * f * f;
        }
    }
    
    public struct Simplex2D<G> : INoise where G : struct, IGradient
    {
        public Sample4 GetNoise4(float4x3 _positions, SmallXXHash4 _hash, int _frequency)
        {
            _positions *= _frequency * (1.0f / sqrt(3.0f));

            float4 skew = (_positions.c0 + _positions.c2) * ((sqrt(3.0f) - 1.0f) / 2.0f);
            
            float4 sx = _positions.c0 + skew;
            float4 sz = _positions.c2 + skew;

            int4 x0 = (int4)floor(sx);
            int4 x1 = x0 + 1;

            int4 z0 = (int4)floor(sz);
            int4 z1 = z0 + 1;

            bool4 xGz = sx - x0 > sz - z0;

            int4 xC = select(x0, x1, xGz);
            int4 zC = select(z1, z0, xGz);

            SmallXXHash4 h0 = _hash.Eat(x0);
            SmallXXHash4 h1 = _hash.Eat(x1);
            SmallXXHash4 hC = SmallXXHash4.Select(h0, h1, xGz);
            
            Sample4 s = default(G).EvaluateCombined(Kernel(h0.Eat(z0), x0, z0, _positions)
                                               + Kernel(h1.Eat(z1), x1, z1, _positions)
                                               + Kernel(hC.Eat(zC), xC, zC, _positions));

            s.dx *= _frequency * (1.0f / sqrt(3.0f));
            s.dz *= _frequency * (1.0f / sqrt(3.0f));

            return s;
        }

        static Sample4 Kernel(SmallXXHash4 _hash, float4 _lx, float4 _lz, float4x3 _positions)
        {
            float4 unskew = (_lx + _lz) * ((3.0f - sqrt(3.0f)) / 6.0f);
            float4 x = _positions.c0 - _lx + unskew;
            float4 z = _positions.c2 - _lz + unskew;
            float4 f = 0.5f - x * x - z * z;

            Sample4 g = default(G).Evaluate(_hash, x, z);

            return new Sample4
            {
                v = f * g.v,
                dx = f * g.dx - 6.0f * x * g.v,
                dz = f * g.dz - 6.0f * z * g.v
            } * f * f * select(0.0f, 8.0f, f >= 0.0f);
        }
    }
    
    public struct Simplex3D<G> : INoise where G : struct, IGradient
    {
        public Sample4 GetNoise4(float4x3 _positions, SmallXXHash4 _hash, int _frequency)
        {
            _positions *= _frequency * 0.6f;

            float4 skew = (_positions.c0 + _positions.c1 + _positions.c2) * (1.0f / 3.0f);
            
            float4 sx = _positions.c0 + skew;
            float4 sy = _positions.c1 + skew;
            float4 sz = _positions.c2 + skew;

            int4 x0 = (int4)floor(sx);
            int4 x1 = x0 + 1;

            int4 y0 = (int4)floor(sy);
            int4 y1 = y0 + 1;

            int4 z0 = (int4)floor(sz);
            int4 z1 = z0 + 1;

            bool4 xGy = sx - x0 > sy - y0;
            bool4 xGz = sx - x0 > sz - z0;
            bool4 yGz = sy - y0 > sz - z0;

            bool4 xA = xGy & xGz;
            bool4 xB = xGy | (xGz & yGz);
            bool4 yA = !xGy & yGz;
            bool4 yB = !xGy | (xGz & yGz);
            bool4 zA = (xGy & !xGz) | (!xGy & !yGz);
            bool4 zB = !(xGz & yGz);

            int4 xCa = select(x0, x1, xA);
            int4 xCb = select(x0, x1, xB);
            int4 yCa = select(y0, y1, yA);
            int4 yCb = select(y0, y1, yB);
            int4 zCa = select(z0, z1, zA);
            int4 zCb = select(z0, z1, zB);

            SmallXXHash4 h0 = _hash.Eat(x0);
            SmallXXHash4 h1 = _hash.Eat(x1);
            SmallXXHash4 hA = SmallXXHash4.Select(h0, h1, xA);
            SmallXXHash4 hB = SmallXXHash4.Select(h0, h1, xB);
            
            Sample4 s = default(G).EvaluateCombined(Kernel(h0.Eat(y0).Eat(z0), x0, y0, z0, _positions)
                                               + Kernel(h1.Eat(y1).Eat(z1), x1, y1, z1, _positions)
                                               + Kernel(hA.Eat(yCa).Eat(zCa), xCa, yCa, zCa, _positions)
                                               + Kernel(hB.Eat(yCb).Eat(zCb), xCb, yCb, zCb, _positions));

            s.dx *= _frequency * 0.6f;
            s.dy *= _frequency * 0.6f;
            s.dz *= _frequency * 0.6f;

            return s;
        }

        static Sample4 Kernel(SmallXXHash4 _hash, float4 _lx, float4 _ly, float4 _lz, float4x3 _positions)
        {
            float4 unskew = (_lx + _ly + _lz) * (1.0f / 6.0f);
            float4 x = _positions.c0 - _lx + unskew;
            float4 y = _positions.c1 - _ly + unskew;
            float4 z = _positions.c2 - _lz + unskew;
            float4 f = 0.5f - x * x - y * y - z * z;

            Sample4 g = default(G).Evaluate(_hash, x, y, z);

            return new Sample4
            {
                v = f * g.v,
                dx = f * g.dx - 6.0f * x * g.v,
                dy = f * g.dy - 6.0f * y * g.v,
                dz = f * g.dz - 6.0f * z * g.v
            } * f * f * select(0.0f, 8.0f, f >= 0.0f);
        }
    }
}