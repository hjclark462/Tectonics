using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFromMesh : MonoBehaviour
{
    TerrainData terrainData;
    Terrain terrain;    
    Bounds bounds;
    float[,] heights;
    

    delegate void CleanUp();

    void CreateTerrain(float[,] inHeights)
    {
        //bounds.size
    }
}
