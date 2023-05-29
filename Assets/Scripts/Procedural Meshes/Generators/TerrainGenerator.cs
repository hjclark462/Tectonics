using System.Diagnostics.Contracts;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public struct TerrainGenerator : IMeshGenerator
{
    // Set the bounds to the center of the mesh
    public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1f, 0f, 1f));

    // The vertex count is one quad times by the rows and columns of the resolution
    public int VertexCount => Width * Height;

    // The index count is two tris (one quad) times by the rows and columns of the resolution
    public int IndexCount => 6 * Width * Height;

    // One job's invocation of Execute generates a row of quads so the job length is defined by the
    // height of the quad, the amount of rows
    public int JobLength => Height + 1;

    public int Width { get; set; }

    public int Height { get; set; }

    public NativeArray<float3> HeightMap { get; set; }

    // The index is the z offset of the row of quads.
    public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
    {
        int vi = (Width +1) * z;
        int ti = 2 * Width * (z - 1);

        Vertex vertex = new Vertex();
        vertex.normal.y = 1f;
        vertex.tangent.xw = float2(1f, -1f);

        // Set the first vertex of the row
        vertex.position.x = HeightMap[vi].x;
        vertex.position.z = HeightMap[vi].y;
        vertex.position.y = HeightMap[vi].z;
        vertex.texCoord0.y = (float)z / Width;
        streams.SetVertex(vi, vertex);
        vi += 1;


        for (int x = 1; x <= Width; x++)
        {
            vertex.position.x = HeightMap[vi].x;            
            vertex.position.y = HeightMap[vi].z;
            vertex.texCoord0.x = (float)x / Width;
            streams.SetVertex(vi, vertex);

            // If it's the first run of the job there won't be any quads to set triangles on so skip it
            if (z > 0)
            {
                streams.SetTriangle(ti, vi + int3(-Width - 2, -1, -Width- 1));
                streams.SetTriangle(ti + 1, vi + int3(-Width - 1, -1, 0));
            }

            vi++;
            ti += 2;
        }
    }
}
