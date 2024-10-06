using Unity.Mathematics;

public readonly struct SmallXXHash
{
    private const uint PrimeA = 0b10011110001101110111100110110001;
    private const uint PrimeB = 0b10000101111010111100101001110111;
    private const uint PrimeC = 0b11000010101100101010111000111101;
    private const uint PrimeD = 0b00100111110101001110101100101111;
    private const uint PrimeE = 0b00010110010101100110011110110001;

    private readonly uint accumulator;

    public SmallXXHash(uint _accumulator)
    {
        accumulator = _accumulator;
    }

    public static implicit operator uint(SmallXXHash _hash)
    {
        uint avalanche = _hash.accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= PrimeB;
        avalanche ^= avalanche >> 13;
        avalanche *= PrimeC;
        avalanche ^= avalanche >> 16;
        
        return avalanche;
    }

    public static implicit operator SmallXXHash(uint _accumulator) => new SmallXXHash(_accumulator);

    public static implicit operator SmallXXHash4(SmallXXHash _hash) => new SmallXXHash4(_hash.accumulator);

    public static SmallXXHash Seed(int _seed) => (uint) _seed + PrimeE;

    static uint RotateLeft(uint _data, int _steps) => (_data << _steps) | (_data >> 32 - _steps);

    public SmallXXHash Eat(int _data) => RotateLeft(accumulator + (uint) _data * PrimeC, 17) * PrimeD;

    public SmallXXHash Eat(byte _data) => RotateLeft(accumulator + _data * PrimeE, 11) * PrimeA;
}

public readonly struct SmallXXHash4
{
    private const uint PrimeB = 0b10000101111010111100101001110111;
    private const uint PrimeC = 0b11000010101100101010111000111101;
    private const uint PrimeD = 0b00100111110101001110101100101111;
    private const uint PrimeE = 0b00010110010101100110011110110001;

    private readonly uint4 accumulator;

    public SmallXXHash4(uint4 _accumulator)
    {
        accumulator = _accumulator;
    }

    public static implicit operator uint4(SmallXXHash4 _hash)
    {
        uint4 avalanche = _hash.accumulator;
        avalanche ^= avalanche >> 15;
        avalanche *= PrimeB;
        avalanche ^= avalanche >> 13;
        avalanche *= PrimeC;
        avalanche ^= avalanche >> 16;
        
        return avalanche;
    }

    public static implicit operator SmallXXHash4(uint4 _accumulator) => new SmallXXHash4(_accumulator);

    public static SmallXXHash4 operator +(SmallXXHash4 _h, int _v) => _h.accumulator + (uint) _v;

    public static SmallXXHash4 Select(SmallXXHash4 _a, SmallXXHash4 _b, bool4 _c) => math.select(_a.accumulator, _b.accumulator, _c);

    public static SmallXXHash4 Seed(int4 _seed) => (uint4) _seed + PrimeE;

    static uint4 RotateLeft(uint4 _data, int _steps) => (_data << _steps) | (_data >> 32 - _steps);

    public SmallXXHash4 Eat(int4 _data) => RotateLeft(accumulator + (uint4) _data * PrimeC, 17) * PrimeD;

    public uint4 GetBits(int _count, int _shift) => ((uint4) this >> _shift) & (uint4) ((1 << _count) - 1);

    public float4 GetBitsAsFloats01(int _count, int _shift) => (float4)GetBits(_count, _shift) * (1.0f / ((1 << _count) - 1));

    public uint4 BytesA => (uint4) this & 255;

    public uint4 BytesB => ((uint4) this >> 8) & 255;

    public uint4 BytesC => ((uint4) this >> 16) & 255;

    public uint4 BytesD => (uint4) this >> 24;

    public float4 Floats01A => (float4) BytesA * (1.0f / 255.0f);

    public float4 Floats01B => (float4) BytesB * (1.0f / 255.0f);

    public float4 Floats01C => (float4) BytesC * (1.0f / 255.0f);

    public float4 Floats01D => (float4) BytesD * (1.0f / 255.0f);
}