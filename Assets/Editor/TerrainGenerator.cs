using UnityEditor;
using UnityEngine;

public class TerrainGenerator : Editor
{
    [MenuItem("Terrain Generator/Generate Terrain")]
    private static void GenerateTerrain()
    {
        var generator = CreateGenerator();
        var terrain = GameObject.FindObjectOfType<Terrain>();
        if (terrain != null)
        {
            terrain.terrainData = generator.GenerateTerrain();
        }
        else
        {
            Terrain.CreateTerrainGameObject(generator.GenerateTerrain());
        }
    }

    static Tectonics CreateGenerator()
    {
        var generator = GameObject.FindObjectOfType<Tectonics>();
        if (generator != null)
        {
            return generator;            
        }
        else
        {
            GameObject tec = new GameObject("Terrain Generator");
            tec.AddComponent<Tectonics>();
            return (tec.GetComponent<Tectonics>());            
        }
    }    
}

