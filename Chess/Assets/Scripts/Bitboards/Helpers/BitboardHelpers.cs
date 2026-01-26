public static class BitboardHelpers
{
#region Bitscanning
    //unity doesnt have bitoperations built in so I implemented debruijns method for bit scanning
    private static readonly int[] DeBruijnIndex64 =
    {
         0,  1, 48,  2, 57, 49, 28,  3,
        61, 58, 50, 42, 38, 29, 17,  4,
        62, 55, 59, 36, 53, 51, 43, 22,
        45, 39, 33, 30, 24, 18, 12,  5,
        63, 47, 56, 27, 60, 41, 37, 16,
        54, 35, 52, 21, 44, 32, 23, 11,
        46, 26, 40, 15, 34, 20, 31, 10,
        25, 14, 19,  9, 13,  8,  7,  6
    };

    private static readonly int[] DeBruijnIndex16 =
    {
        0,  1,  2,  5,  3,  9,  6, 11,
        15,  4,  8, 10, 14,  7, 13, 12
    };

    private const ulong DeBruijn64 = 0x03f79d71b4cb0a89UL;
    private const ushort DeBruijn16 = 0x09AF;

    private static int BitScanForward(ulong bb)
    {
        return DeBruijnIndex64[((bb & (~bb + 1)) * DeBruijn64) >> 58];
    }
    private static int BitScanForward(ushort bb)
    {
        ushort lsb = (ushort)(bb & (ushort)(-(short)bb));
        return DeBruijnIndex16[((lsb * DeBruijn16) >> 12) & 0xF];
    }
    public static int PopLeastSignificantBit(ref ulong bb)
    {
        int square = BitScanForward(bb);
        bb &= bb - 1;
        return square;
    }
    public static int PopLeastSignificantBit(ref ushort bb)
    {
        int square = BitScanForward(bb);
        bb &= (ushort)(bb - 1);
        return square;
    }
    #endregion
    public static int CountBits(ulong bb)
    {
        int count = 0;
        while (bb != 0)
        {
            bb &= bb - 1; //clear the least significant bit set
            count++;
        }
        return count;
    }
}
