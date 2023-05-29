using Unity.Mathematics;

// Struct to hold the data while the Job is being executedby Burst
public struct Vertex
{
    public float3 position;
    public float3 normal;
    public float4 tangent;
    public float2 texCoord0;
}
