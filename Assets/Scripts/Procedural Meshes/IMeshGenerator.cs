using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

// Base generator interface which is the different shaped generators inherit from
public interface IMeshGenerator
{
    // Getters for each values needed to produce the varying mesh types that are defined in the 
    // inherited generators
    Bounds Bounds { get; }

    int VertexCount { get; }

    int IndexCount { get; }

    int JobLength { get; }

    int Height { get; set; }
    int Width { get; set; }

    NativeArray<float3> HeightMap { get; set; }

    // Executed by the MeshJob struct with an index parameter and an IMeshStreams struct used for 
    // defining the order the data is stored in memory
    void Execute<S>(int i, S streams) where S : struct, IMeshStreams;
}
