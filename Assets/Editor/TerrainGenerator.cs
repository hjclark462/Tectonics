using UnityEditor;
using UnityEngine;

public class TerrainGenerator : Editor
{
    [MenuItem("Terrain Generator/Generate Terrain")]
    private static void GenerateTerrain()
    {
        var terrain = GameObject.FindObjectOfType<Terrain>();
        if (terrain != null)
        {
            GameObject.DestroyImmediate(terrain.gameObject, true);
        }
        var generator = GameObject.FindObjectOfType<Tectonics>();
        if (generator != null)
        {
            Terrain.CreateTerrainGameObject(generator.GenerateTerrain());
            generator.CleanUp();
        }
        else
        {
            GameObject tec = new GameObject("Terrain Generator");
            tec.AddComponent<Tectonics>();
            Terrain.CreateTerrainGameObject(tec.GetComponent<Tectonics>().GenerateTerrain());
            tec.GetComponent<Tectonics>().CleanUp();
        }
    }
}

