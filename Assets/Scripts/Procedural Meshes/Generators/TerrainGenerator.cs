using System.Diagnostics.Contracts;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public struct TerrainGenerator : IMeshGenerator
{
    // Set the bounds to the center of the rhombic quad
    public Bounds Bounds => new Bounds(Vector3.zero, new Vector3(1f + 0.5f / Resolution, 0f, sqrt(3f) / 2f));

    public int VertexCount => (Resolution + 1) * (Resolution + 1);

    public int IndexCount => 6 * Resolution * Resolution;

    public int JobLength => Resolution + 1;

    public int Resolution { get; set; }

    public void Execute<S>(int z, S streams) where S : struct, IMeshStreams
    {
        int vi = (Resolution + 1) * z;
        int ti = 2 * Resolution * (z - 1);

        // offset both the x axis of the vertex positon and the u texture coordinates to line up 
        // with the slant that happens from the generated rhombus.
        float xOffset = -0.25f;
        float uOffset = 0f;

        int iA = -Resolution - 2;
        int iB = -Resolution - 1;
        int iC = -1;
        int iD = 0;

        int3 tA = int3(iA, iC, iD);
        int3 tB = int3(iA, iD, iB);

        // Bitwise operator to check for odd numbered rows so that the information can be shifted to another orientation
        // that happens on the odd rows. 
        if ((z & 1) == 1)
        {
            xOffset = 0.25f;
            uOffset = 0.5f / (Resolution + 0.5f);
            tA = int3(iA, iC, iB);
            tB = int3(iB, iC, iD);
        }

        // Keep the grid centered
        xOffset = xOffset / Resolution - 0.5f;

        Vertex vertex = new Vertex();
        vertex.normal.y = 1f;
        vertex.tangent.xw = float2(1f, -1f);

        // Set the first vertex of the row using the offset to make it an equilateral tri and
        // adjusting the texture coords accordingly.
        vertex.position.x = xOffset;
        vertex.position.z = ((float)z / Resolution - 0.5f) * sqrt(3f) / 2f;
        vertex.texCoord0.x = uOffset;
        vertex.texCoord0.y = vertex.position.z / (1f + 0.5f / Resolution) + 0.5f;
        streams.SetVertex(vi, vertex);
        vi++;

        for (int x = 1; x <= Resolution; x++)
        {
            vertex.position.x = (float)x / Resolution + xOffset;
            vertex.texCoord0.x = x / (Resolution + 0.5f) + uOffset;
            streams.SetVertex(vi, vertex);

            // Only form quads on the second row of verts.
            if (z > 0)
            {
                streams.SetTriangle(ti + 0, vi + tA);
                streams.SetTriangle(ti + 1, vi + tB);
            }
            vi++;
            ti += 2;
        }
    }
}
