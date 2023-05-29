using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

// Single stream approach of the IMeshStream interface
// setting the attributs in order grouped by vertex
public struct SingleStream : IMeshStreams
{
    // Seqeuential struct to store the vertex attributes in order
    [StructLayout(LayoutKind.Sequential)]
    struct Stream0
    {
        public float3 position;
        public float3 normal;
        public float4 tangent;
        public float2 texCoord0;
    }

    [NativeDisableContainerSafetyRestriction]
    NativeArray<Stream0> stream0;

    [NativeDisableContainerSafetyRestriction]
    NativeArray<TriangleUInt16> triangles;

    // Definition of the Setup func from the base IMeshStreams class. Defines the mesh buffers, 
    // the bounds and the sub-mesh
    public void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount)
    {
        // Temporary native array of four VertexAttributeDescriptor, one for each of the attributes
        // stored in the vertex of the mesh being generated. The dimension argument indicates the number
        // of values that is attached to that attribute
        var descriptor = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

        // Default Vertex Attribute is position so it does not need definition like the remaining three.
        descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
        descriptor[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, dimension: 3);
        descriptor[2] = new VertexAttributeDescriptor(VertexAttribute.Tangent, dimension: 4);
        descriptor[3] = new VertexAttributeDescriptor(VertexAttribute.TexCoord0, dimension: 2);

        // Allocate the stream of the mesh to the meshData, then as they are allocated and no longer
        // needed dispose of them and free up memory.
        meshData.SetVertexBufferParams(vertexCount, descriptor);
        descriptor.Dispose();

        meshData.SetIndexBufferParams(indexCount, IndexFormat.UInt16);

        // Assign SubMesh and then add the bounds to it
        meshData.subMeshCount = 1;
        meshData.SetSubMesh(0, new SubMeshDescriptor(0, indexCount)
        {
            bounds = bounds,
            vertexCount = vertexCount
        },
            MeshUpdateFlags.DontRecalculateBounds |
            MeshUpdateFlags.DontValidateIndices);

        // Set the vertex buffer format
        stream0 = meshData.GetVertexData<Stream0>();
        // Set the index element type and the buffer format
        triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
    }

    // Copies the vertex data to Stream0 struct at the index supplied as the argument
    // So that Burst inserts the code inline instead of going for a call the MehodImpl
    // attribute is attached to force it.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVertex(int index, Vertex vertex) => stream0[index] = new Stream0
    {
        position = vertex.position,
        normal = vertex.normal,
        tangent = vertex.tangent,
        texCoord0 = vertex.texCoord0
    };

    // Copies the triangle data to the index buffer
    public void SetTriangle(int index, int3 triangle) => triangles[index] = triangle;
}
