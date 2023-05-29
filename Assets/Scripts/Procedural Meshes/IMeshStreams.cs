using Unity.Mathematics;
using UnityEngine;

// Base interface to define the vertex and index buffers so the relevant data can
// be placed in the MeshData in the appropriate format. Multiple types can be made from this.
public interface IMeshStreams
{
    // Initialize the mesh data with the desired vertex and index counts		
    void Setup(Mesh.MeshData meshData, Bounds bounds, int vertexCount, int indexCount);

    // Copy a vertex to the vertex buffer
    void SetVertex(int index, Vertex vertex);

    // Set the index buffer using a vertex index triplet
    void SetTriangle(int index, int3 triangle);
}
