using Unity.Mathematics;

public static partial class Noise
{
    public interface IVoronoiFunction
    {
        Sample4 Evaluate(VoronoiData _data);
    }

    public struct F1 : IVoronoiFunction
    {
        public Sample4 Evaluate(VoronoiData _data) => _data.a;
    }

    public struct F2 : IVoronoiFunction
    {
        public Sample4 Evaluate(VoronoiData _data) => _data.b;
    }

    public struct F2MinusF1 : IVoronoiFunction
    {
        public Sample4 Evaluate(VoronoiData _data) => _data.b - _data.a;
    }
}