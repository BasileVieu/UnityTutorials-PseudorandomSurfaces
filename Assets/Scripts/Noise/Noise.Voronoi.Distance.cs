using Unity.Mathematics;
using static Unity.Mathematics.math;

public static partial class Noise
{
    public interface IVoronoiDistance
    {
        VoronoiData UpdateVoronoiData(VoronoiData _data, Sample4 _sample);

        VoronoiData InitialData
        {
            get;
        }
        
        Sample4 GetDistance(float4 _x);
        
        Sample4 GetDistance(float4 _x, float4 _y);
        
        Sample4 GetDistance(float4 _x, float4 _y, float4 _z);

        VoronoiData Finalize1D(VoronoiData _data);

        VoronoiData Finalize2D(VoronoiData _data);

        VoronoiData Finalize3D(VoronoiData _data);
    }

    public struct Worley : IVoronoiDistance
    {
        public VoronoiData UpdateVoronoiData(VoronoiData _data, Sample4 _sample)
        {
            bool4 newMinimum = _sample.v < _data.a.v;

            _data.b = Sample4.Select(Sample4.Select(_data.b, _sample, _sample.v < _data.b.v), _data.a, newMinimum);

            _data.a = Sample4.Select(_data.a, _sample, newMinimum);

            return _data;
        }

        public VoronoiData InitialData => new VoronoiData
        {
            a = new Sample4
            {
                v = 2.0f
            },
            b = new Sample4
            {
                v = 2.0f
            }
        };
        
        public Sample4 GetDistance(float4 _x) => new Sample4
        {
            v = abs(_x),
            dx = select(-1.0f, 1.0f, _x < 0.0f)
        };

        public Sample4 GetDistance(float4 _x, float4 _y) => GetDistance(_x, 0.0f, _y);

        public Sample4 GetDistance(float4 _x, float4 _y, float4 _z) => new Sample4
        {
            v = _x * _x + _y * _y + _z * _z,
            dx = _x,
            dy = _y,
            dz = _z
        };

        public VoronoiData Finalize1D(VoronoiData _data) => _data;

        public VoronoiData Finalize2D(VoronoiData _data) => Finalize3D(_data);

        public VoronoiData Finalize3D(VoronoiData _data)
        {
            bool4 keepA = _data.a.v < 1.0f;
            _data.a.v = select(1.0f, sqrt(_data.a.v), keepA);
            _data.a.dx = select(0.0f, -_data.a.dx / _data.a.v, keepA);
            _data.a.dy = select(0.0f, -_data.a.dy / _data.a.v, keepA);
            _data.a.dz = select(0.0f, -_data.a.dz / _data.a.v, keepA);

            bool4 keepB = _data.b.v < 1.0f;
            _data.b.v = select(1.0f, sqrt(_data.b.v), keepB);
            _data.b.dx = select(0.0f, -_data.b.dx / _data.b.v, keepB);
            _data.b.dy = select(0.0f, -_data.b.dy / _data.b.v, keepB);
            _data.b.dz = select(0.0f, -_data.b.dz / _data.b.v, keepB);

            return _data;
        }
    }

    public struct SmoothWorley : IVoronoiDistance
    {
        private const float smoothLSE = 10.0f;
        private const float smoothPoly = 0.25f;

        public VoronoiData UpdateVoronoiData(VoronoiData _data, Sample4 _sample)
        {
            float4 e = exp(-smoothLSE * _sample.v);

            _data.a.v += e;
            _data.a.dx += e * _sample.dx;
            _data.a.dy += e * _sample.dy;
            _data.a.dz += e * _sample.dz;

            float4 h = 1.0f - abs(_data.b.v - _sample.v) / smoothPoly;

            float4 hdx = _data.b.dx - _sample.dx;
            float4 hdy = _data.b.dy - _sample.dy;
            float4 hdz = _data.b.dz - _sample.dz;

            bool4 ds = _data.b.v - _sample.v < 0.0f;

            hdx = select(-hdx, hdx, ds) * 0.5f * h;
            hdy = select(-hdy, hdy, ds) * 0.5f * h;
            hdz = select(-hdz, hdz, ds) * 0.5f * h;

            bool4 smooth = h > 0.0f;

            h = 0.25f * smoothPoly * h * h;

            _data.b = Sample4.Select(_data.b, _sample, _sample.v < _data.b.v);
            _data.b.v -= select(0.0f, h, smooth);
            _data.b.dx -= select(0.0f, hdx, smooth);
            _data.b.dy -= select(0.0f, hdy, smooth);
            _data.b.dz -= select(0.0f, hdz, smooth);

            return _data;
        }

        public VoronoiData InitialData => new VoronoiData
        {
            b = new Sample4
            {
                v = 2.0f
            }
        };
        
        public Sample4 GetDistance(float4 _x) => default(Worley).GetDistance(_x);
        
        public Sample4 GetDistance(float4 _x, float4 _y) => GetDistance(_x, 0.0f, _y);

        public Sample4 GetDistance(float4 _x, float4 _y, float4 _z)
        {
            float4 v = sqrt(_x * _x + _y * _y + _z * _z);

            return new Sample4
            {
                v = v,
                dx = _x / -v,
                dy = _y / -v,
                dz = _z / -v
            };
        }

        public VoronoiData Finalize1D(VoronoiData _data)
        {
            _data.a.dx /= _data.a.v;
            _data.a.v = log(_data.a.v) / -smoothLSE;
            _data.a = Sample4.Select(default, _data.a.Smoothstep, _data.a.v > 0.0f);
            _data.b = Sample4.Select(default, _data.b.Smoothstep, _data.b.v > 0.0f);

            return _data;
        }

        public VoronoiData Finalize2D(VoronoiData _data) => Finalize3D(_data);

        public VoronoiData Finalize3D(VoronoiData _data)
        {
            _data.a.dx /= _data.a.v;
            _data.a.dy /= _data.a.v;
            _data.a.dz /= _data.a.v;
            _data.a.v = log(_data.a.v) / -smoothLSE;
            _data.a = Sample4.Select(default, _data.a.Smoothstep, _data.a.v > 0.0f & _data.a.v < 1.0f);
            _data.b = Sample4.Select(default, _data.b.Smoothstep, _data.b.v > 0.0f & _data.b.v < 1.0f);

            return _data;
        }
    }

    public struct Chebyshev : IVoronoiDistance
    {
        public VoronoiData UpdateVoronoiData(VoronoiData _data, Sample4 _sample) =>
            default(Worley).UpdateVoronoiData(_data, _sample);

        public VoronoiData InitialData => default(Worley).InitialData;
        
        public Sample4 GetDistance(float4 _x) => default(Worley).GetDistance(_x);

        public Sample4 GetDistance(float4 _x, float4 _y)
        {
            bool4 keepX = abs(_x) > abs(_y);

            return new Sample4
            {
                v = select(abs(_y), abs(_x), keepX),
                dx = select(0.0f, select(-1.0f, 1.0f, _x < 0.0f), keepX),
                dz = select(select(-1.0f, 1.0f, _y < 0.0f), 0.0f, keepX)
            };
        }

        public Sample4 GetDistance(float4 _x, float4 _y, float4 _z)
        {
            bool4 keepX = abs(_x) > abs(_y) & abs(_x) > abs(_z);
            bool4 keepY = abs(_y) > abs(_z);

            return new Sample4
            {
                v = select(select(abs(_z), abs(_y), keepY), abs(_x), keepX),
                dx = select(0.0f, select(-1.0f, 1.0f, _x < 0.0f), keepX),
                dy = select(select(0.0f, select(-1.0f, 1.0f, _y < 0.0f), keepY), 0.0f, keepX),
                dz = select(select(select(-1.0f, 1.0f, _z < 0.0f), 0.0f, keepY), 0.0f, keepX)
            };
        }

        public VoronoiData Finalize1D(VoronoiData _data) => _data;

        public VoronoiData Finalize2D(VoronoiData _data) => _data;

        public VoronoiData Finalize3D(VoronoiData _data) => _data;
    }
}