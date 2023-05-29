using System.Runtime.InteropServices;
using Unity.Mathematics;

// Sequential struct in memory using ushort to store triangle indices as 16 bit
// to half the index buffer size.
[StructLayout(LayoutKind.Sequential)]
public struct TriangleUInt16
{
    public ushort a, b, c;

    // Conversion from int3 to this struct
    public static implicit operator TriangleUInt16(int3 t) => new TriangleUInt16
    {
        a = (ushort)t.x,
        b = (ushort)t.y,
        c = (ushort)t.z
    };
}
