using System.IO.Compression;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static partial class Noise
{
    public struct LatticeSpan4
    {
        public int4 p0;
        public int4 p1;
        public float4 g0;
        public float4 g1;
        public float4 t;
        public float4 dt;
    }

    public interface ILattice
    {
        LatticeSpan4 GetLatticeSpan4(float4 _coordinates, int _frequency);

        int4 ValidateSingleStep(int4 _points, int _frequency);
    }

    public struct LatticeNormal : ILattice
    {
        public LatticeSpan4 GetLatticeSpan4(float4 _coordinates, int _frequency)
        {
            _coordinates *= _frequency;

            float4 points = floor(_coordinates);

            LatticeSpan4 span;
            span.p0 = (int4)points;
            span.p1 = span.p0 + 1;
            span.g0 = _coordinates - span.p0;
            span.g1 = span.g0 - 1.0f;

            float4 t = _coordinates - points;

            span.t = t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
            span.dt = t * t * (t * (t * 30.0f - 60.0f) + 30.0f);

            return span;
        }

        public int4 ValidateSingleStep(int4 _points, int _frequency) => _points;
    }

    public struct LatticeTiling : ILattice
    {
        public LatticeSpan4 GetLatticeSpan4(float4 _coordinates, int _frequency)
        {
            _coordinates *= _frequency;

            float4 points = floor(_coordinates);

            LatticeSpan4 span;
            span.p0 = (int4)points;
            span.g0 = _coordinates - span.p0;
            span.g1 = span.g0 - 1.0f;

            span.p0 -= (int4)(points / _frequency) * _frequency;
            span.p0 = select(span.p0, span.p0 + _frequency, span.p0 < 0);
            span.p1 = span.p0 + 1;
            span.p1 = select(span.p1, 0, span.p1 == _frequency);

            float4 t = _coordinates - points;

            span.t = t * t * t * (t * (t * 6.0f - 15.0f) + 10.0f);
            span.dt = t * t * (t * (t * 30.0f - 60.0f) + 30.0f);

            return span;
        }

        public int4 ValidateSingleStep(int4 _points, int _frequency) =>
            select(select(_points, 0, _points == _frequency), _frequency - 1, _points == -1);
    }

    public struct Lattice1D<L, G> : INoise where L : struct, ILattice where G : struct, IGradient
    {
        public Sample4 GetNoise4(float4x3 _positions, SmallXXHash4 _hash, int _frequency)
        {
            LatticeSpan4 x = default(L).GetLatticeSpan4(_positions.c0, _frequency);

            G g = default(G);

            Sample4 a = g.Evaluate(_hash.Eat(x.p0), x.g0);
            Sample4 b = g.Evaluate(_hash.Eat(x.p1), x.g1);

            return g.EvaluateCombined(new Sample4
            {
                v = lerp(a.v, b.v, x.t),
                dx = _frequency * (lerp(a.dx, b.dx, x.t) + (b.v - a.v) * x.dt)
            });
        }
    }

    public struct Lattice2D<L, G> : INoise where L : struct, ILattice where G : struct, IGradient
    {
        public Sample4 GetNoise4(float4x3 _positions, SmallXXHash4 _hash, int _frequency)
        {
            L l = default(L);

            LatticeSpan4 x = l.GetLatticeSpan4(_positions.c0, _frequency);
            LatticeSpan4 z = l.GetLatticeSpan4(_positions.c2, _frequency);

            SmallXXHash4 h0 = _hash.Eat(x.p0);
            SmallXXHash4 h1 = _hash.Eat(x.p1);

            G g = default(G);

            Sample4 a = g.Evaluate(h0.Eat(z.p0), x.g0, z.g0);
            Sample4 b = g.Evaluate(h0.Eat(z.p1), x.g0, z.g1);
            Sample4 c = g.Evaluate(h1.Eat(z.p0), x.g1, z.g0);
            Sample4 d = g.Evaluate(h1.Eat(z.p1), x.g1, z.g1);

            return g.EvaluateCombined(new Sample4
            {
                v = lerp(lerp(a.v, b.v, z.t), lerp(c.v, d.v, z.t), x.t),
                dx = _frequency *
                     (lerp(lerp(a.dx, b.dx, z.t), lerp(c.dx, d.dx, z.t), x.t) +
                      (lerp(c.v, d.v, z.t) - lerp(a.v, b.v, z.t)) * x.dt),
                dz = _frequency * lerp(
                    lerp(a.dz, b.dz, z.t) + (b.v - a.v) * z.dt,
                    lerp(c.dz, d.dz, z.t) + (d.v - c.v) * z.dt,
                    x.t)
            });
        }
    }

    public struct Lattice3D<L, G> : INoise where L : struct, ILattice where G : struct, IGradient
    {
        public Sample4 GetNoise4(float4x3 _positions, SmallXXHash4 _hash, int _frequency)
        {
            L l = default(L);

            LatticeSpan4 x = l.GetLatticeSpan4(_positions.c0, _frequency);
            LatticeSpan4 y = l.GetLatticeSpan4(_positions.c1, _frequency);
            LatticeSpan4 z = l.GetLatticeSpan4(_positions.c2, _frequency);

            SmallXXHash4 h0 = _hash.Eat(x.p0);
            SmallXXHash4 h1 = _hash.Eat(x.p1);
            SmallXXHash4 h00 = h0.Eat(y.p0);
            SmallXXHash4 h01 = h0.Eat(y.p1);
            SmallXXHash4 h10 = h1.Eat(y.p0);
            SmallXXHash4 h11 = h1.Eat(y.p1);

            G gradient = default(G);

            Sample4 a = gradient.Evaluate(h00.Eat(z.p0), x.g0, y.g0, z.g0);
            Sample4 b = gradient.Evaluate(h00.Eat(z.p1), x.g0, y.g0, z.g1);
            Sample4 c = gradient.Evaluate(h01.Eat(z.p0), x.g0, y.g1, z.g0);
            Sample4 d = gradient.Evaluate(h01.Eat(z.p1), x.g0, y.g1, z.g1);
            Sample4 e = gradient.Evaluate(h10.Eat(z.p0), x.g1, y.g0, z.g0);
            Sample4 f = gradient.Evaluate(h10.Eat(z.p1), x.g1, y.g0, z.g1);
            Sample4 g = gradient.Evaluate(h11.Eat(z.p0), x.g1, y.g1, z.g0);
            Sample4 h = gradient.Evaluate(h11.Eat(z.p1), x.g1, y.g1, z.g1);

            return gradient.EvaluateCombined(new Sample4
            {
                v = lerp(
                    lerp(lerp(a.v, b.v, z.t), lerp(c.v, d.v, z.t), y.t),
                    lerp(lerp(e.v, f.v, z.t), lerp(g.v, h.v, z.t), y.t),
                    x.t),
                dx = _frequency *
                    lerp(lerp(lerp(a.dx, b.dx, z.t), lerp(c.dx, d.dx, z.t), y.t),
                        lerp(lerp(e.dx, f.dx, z.t), lerp(g.dx, h.dx, z.t), y.t),
                        x.t) + (
                        lerp(lerp(e.v, f.v, z.t), lerp(g.v, h.v, z.t), y.t) -
                        lerp(lerp(a.v, b.v, z.t), lerp(c.v, d.v, z.t), y.t)
                        * x.dt),
                dy = _frequency * lerp(
                    lerp(lerp(a.dy, b.dy, z.t), lerp(c.dy, d.dy, z.t), y.t) +
                    (lerp(c.v, d.v, z.t) - lerp(a.v, b.v, z.t)) * y.dt,
                    lerp(lerp(e.dy, f.dy, z.t), lerp(g.dy, h.dy, z.t), y.t) +
                    (lerp(g.v, h.v, z.t) - lerp(e.v, f.v, z.t)) * y.dt,
                    x.t),
                dz = _frequency * lerp(
                    lerp(lerp(a.dz, b.dz, z.t) + (b.v - a.v) * z.dt,
                        lerp(c.dz, d.dz, z.t) + (d.v - c.v) * z.dt, y.t),
                    lerp(lerp(e.dz, b.dz, z.t) + (f.v - e.v) * z.dt,
                        lerp(g.dz, h.dz, z.t) + (h.v - g.v) * z.dt, y.t),
                    x.t)
            });
        }
    }
}