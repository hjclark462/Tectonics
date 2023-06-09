using System.Collections.Generic;
using TerrainGenHelpers;
using UnityEngine;

public class Tectonics : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Check this to run the generator on play. If false can still generate using Tools: Generate Terrain. " +
        "Beware this can override the current visual from editor to play if random plates is selected")]
    bool m_runtime = false;

    [SerializeField]
    [Tooltip("Will generate a pseudorandom height terrain using the below values. If selected Plate Objects does not need to be filled" +
        "to generate Terrain and will fill the list upon creation")]
    bool m_randomPlates = true;

    [SerializeField]
    [Tooltip("List of Scriptable Objects that can be created from the Asset Menu under Terrain Generator: Plate. If Random Plates " +
        "is unchecked the amount of plates in the generated terrain will be the amount in this list")]
    List<PlateSO> m_plateObjects;

    [SerializeField]
    int plateAmount = 12;

    [SerializeField]
    [Tooltip("The X and Y (minus 1 texel) dimensions of the height map being generated for the terrain")]
    TerrainResolutions m_heightMapResolution = TerrainResolutions._128;

    [SerializeField]
    [Tooltip("The resolution of the composite Texture to use on the Terrain when you view it from a distance greater than the Basemap Distance.")]
    TerrainResolutions m_baseMapResolution = TerrainResolutions._128;

    [SerializeField]
    [Tooltip("The resolution of the splatmap that controls the blending of the different Terrain Textures.")]
    TerrainResolutions m_controlMapResolution = TerrainResolutions._128;

    [SerializeField]
    [Tooltip("The number of cells available for placing details onto the Terrain tile. This value is squared to make a grid of cells.")]
    TerrainResolutions m_detailMapResolution = TerrainResolutions._128;

    [SerializeField]
    [Tooltip("The length of the X and Z axis of the generated Terrain in World Units")]
    int m_terrainResolution = 128;

    [SerializeField]
    [Tooltip("How high the terrain will be in world units. Terrain Y axis values are the height map 0-1 values" +
        " timesed by this value")]
    int m_heightScale = 100;
        
    [SerializeField]
    [Tooltip("Top of range of height values to spread from. Will translate to 1 on the height map")]
    int m_maxElevation = 1000;
    [SerializeField]
    [Tooltip("Bottom of range of height values to spread from. Will translate to 0 on the height map")]
    int m_minElevation = -1000;

    [SerializeField]
    [Tooltip("The amount of smoothing on the terrain after the spreading of the plates. The higher the value the longer" +
        "the processing time on generation can become")]
    int smoothAmount = 100;

    [SerializeField]
    [Tooltip("If you want to save a png texture of the coloured plates for reference. Saved to Assets/TerrainGenerator/Textures")]
    bool m_saveColourMap = false;
    [SerializeField]
    [Tooltip("If you want to save a png texture of the heightMap for reference. Saved to Assets/TerrainGenerator/Textures")]
    bool m_saveHeightMap = false;

    RenderTexture JFACalculation;
    RenderTexture JFAResult;

    RenderTexture PlateTracker;
    RenderTexture PlateResult;

    RenderTexture HeightMap;
            
    TerrainData tData;    

    ComputeShader jumpFill;
    ComputeBuffer plateBuffer;
    ComputeBuffer pointBuffer;
    ComputeBuffer colourBuffer;

    Point[] plates;
    Point[] points;
    Vector4[] colours;
    float[,] terrainHeights;

    int initPlateKernel;
    int jumpFillKernel;
    int jFAColoursKernel;
    int setPointDataKernel;
    int smoothElevationKernel;
    int setHeightMapKernel;
    int testWorldColoursKernel;    

    int threadGroupsX;
    int threadGroupsY;    

    void Start()
    {        
        if (m_runtime)
        {
            Terrain.CreateTerrainGameObject(GenerateTerrain());
            CleanUp();
        }
    }   

    public void CleanUp()
    {
        plateBuffer.Release();
        pointBuffer.Release();
        colourBuffer.Release();
        plateBuffer = null;
        pointBuffer = null;
        colourBuffer = null;
    }

    public TerrainData GenerateTerrain()
    {        
        InitTextures();
        InitBuffersKernel();
        InitPlateSeeds();
        JumpFloodAlgorithm();
        GetPointData();
        for (int i = 0; i < smoothAmount; i++)
        {
            SmoothElevation();
        }
        TestWorldColours();
        GetHeightMap();
        SetTerrainData();
        if(m_saveColourMap)
        {
            GetPlateMap();
        }
        return tData;
    }

    void InitTextures()
    {
        JFACalculation = new RenderTexture((int)m_heightMapResolution, (int)m_heightMapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        JFACalculation.name = "JFACalculation";
        JFACalculation.enableRandomWrite = true;
        JFACalculation.Create();
        JFAResult = new RenderTexture((int)m_heightMapResolution, (int)m_heightMapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        JFAResult.name = "JFAResult";
        JFAResult.enableRandomWrite = true;
        JFAResult.Create();
        PlateTracker = new RenderTexture((int)m_heightMapResolution, (int)m_heightMapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        PlateTracker.name = "PlateTracker";
        PlateTracker.enableRandomWrite = true;
        PlateTracker.Create();
        PlateResult = new RenderTexture((int)m_heightMapResolution, (int)m_heightMapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        PlateResult.name = "PlateResult";
        PlateResult.enableRandomWrite = true;
        PlateResult.Create();
        HeightMap = new RenderTexture((int)m_heightMapResolution, (int)m_heightMapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        HeightMap.name = "HeightMap";
        HeightMap.enableRandomWrite = true;
        HeightMap.Create();

        threadGroupsX = Mathf.CeilToInt(JFACalculation.width / 8.0f);
        threadGroupsY = Mathf.CeilToInt(JFACalculation.height / 8.0f);
    }

    void InitBuffersKernel()
    {
        jumpFill = (ComputeShader)Resources.Load("TerrainGeneration/JumpFill");
        terrainHeights = new float[(int)m_heightMapResolution + 1, (int)m_heightMapResolution + 1];
        points = new Point[PlateTracker.width * PlateTracker.height];

        if (m_randomPlates)
        {
            m_plateObjects = new List<PlateSO>();
            plates = new Point[plateAmount];
            colours = new Vector4[plateAmount];

            for (int i = 0; i < plateAmount; i++)
            {
                Point p = new Point();
                PlateSO plateSO = PlateSO.CreateInstance<PlateSO>();
                p.position = plateSO.coordinate = new Vector2Int(Random.Range(0, JFACalculation.width - 1), Random.Range(0, JFACalculation.height - 1));
                p.plate = i;
                int pT = Random.Range(0, 20000);
                if (pT < 10000)
                {
                    p.plateType = 0;
                }
                else
                {
                    p.plateType = 1;
                }
                if (p.plateType == 1)
                {
                    p.elevation = plateSO.elevation = Random.Range(0.0f, m_maxElevation);
                }
                else
                {
                    p.elevation = plateSO.elevation = Random.Range(m_minElevation, 0.0f);
                }
                p.elevation = Remap(p.elevation);
                colours[i] = plateSO.color = new Vector4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);

                plates[i] = p;
                m_plateObjects.Add(plateSO);
            }
        }
        else
        {
            plateAmount = m_plateObjects.Count;
            plates = new Point[plateAmount];
            colours = new Vector4[plateAmount];

            for (int i = 0; i < plateAmount; i++)
            {
                Point p = new Point();
                p.position = m_plateObjects[i].coordinate;
                p.plate = i;
                p.elevation = m_plateObjects[i].elevation;            
                p.elevation = Remap(p.elevation);
                colours[i] = m_plateObjects[i].color;
                plates[i] = p;
            }
        }

        int pointSize = (sizeof(float) + sizeof(int) * 4);
        plateBuffer = new ComputeBuffer(plateAmount, pointSize);
        plateBuffer.SetData(plates);
        pointBuffer = new ComputeBuffer(points.Length + 1, pointSize, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        pointBuffer.SetData(points);        
        colourBuffer = new ComputeBuffer(plateAmount, sizeof(float) * 4);
        colourBuffer.SetData(colours);

        initPlateKernel = jumpFill.FindKernel("InitPlate");
        jumpFillKernel = jumpFill.FindKernel("JumpFill");
        setPointDataKernel = jumpFill.FindKernel("SetPointData");
        smoothElevationKernel = jumpFill.FindKernel("SmoothElevation");
        setHeightMapKernel = jumpFill.FindKernel("SetHeightMap");        
        jFAColoursKernel = jumpFill.FindKernel("TestJFAColours");        

        jumpFill.SetInt("maxHeight", m_maxElevation);
        jumpFill.SetInt("minHeight", m_minElevation);
        jumpFill.SetInt("width", JFACalculation.width);
        jumpFill.SetInt("height", JFACalculation.height);
    }

    void InitPlateSeeds()
    {
        plateBuffer.SetData(plates);
        jumpFill.SetBuffer(initPlateKernel, "plates", plateBuffer);
        jumpFill.SetTexture(initPlateKernel, "JFACalculation", JFACalculation);
        jumpFill.SetTexture(initPlateKernel, "JFAResult", JFAResult);
        jumpFill.Dispatch(initPlateKernel, plateAmount, 1, 1);
    }

    void JumpFloodAlgorithm()
    {
        int stepAmount = (int)Mathf.Log(Mathf.Max(JFACalculation.width, JFACalculation.height), 2);

        for (int i = 0; i < stepAmount; i++)
        {
            int step = (int)Mathf.Pow(2, stepAmount - i - 1);
            jumpFill.SetInt("step", step);
            jumpFill.SetTexture(jumpFillKernel, "JFACalculation", JFACalculation);
            jumpFill.SetTexture(jumpFillKernel, "JFAResult", JFAResult);
            jumpFill.Dispatch(jumpFillKernel, threadGroupsX, threadGroupsY, 1);
            Graphics.Blit(JFAResult, JFACalculation);
        }
    }

    void GetPointData()
    {
        plateBuffer.SetData(plates);
        pointBuffer.SetData(points);
        jumpFill.SetBuffer(setPointDataKernel, "plates", plateBuffer);
        jumpFill.SetBuffer(setPointDataKernel, "points", pointBuffer);
        jumpFill.SetTexture(setPointDataKernel, "JFACalculation", JFACalculation);
        jumpFill.SetTexture(setPointDataKernel, "PlateTracker", PlateTracker);
        jumpFill.SetInt("width", JFACalculation.width);
        jumpFill.Dispatch(setPointDataKernel, threadGroupsX, threadGroupsY, 1);
        pointBuffer.GetData(points);
    }

    void SmoothElevation()
    {
        pointBuffer.SetData(points);
        jumpFill.SetBuffer(smoothElevationKernel, "points", pointBuffer);
        jumpFill.SetTexture(smoothElevationKernel, "PlateTracker", PlateTracker);
        jumpFill.SetTexture(smoothElevationKernel, "PlateResult", PlateResult);
        jumpFill.SetInt("width", PlateTracker.width);
        jumpFill.SetInt("height", PlateTracker.height);
        jumpFill.Dispatch(smoothElevationKernel, threadGroupsX, threadGroupsY, 1);
        pointBuffer.GetData(points);
        Graphics.Blit(PlateResult, PlateTracker);
    }
    
    void GetPlateMap()
    {
        colourBuffer.SetData(colours);
        jumpFill.SetBuffer(jFAColoursKernel, "colours", colourBuffer);
        jumpFill.SetTexture(jFAColoursKernel, "JFACalculation", JFACalculation);
        jumpFill.SetTexture(jFAColoursKernel, "JFAResult", JFAResult);
        jumpFill.Dispatch(jFAColoursKernel, threadGroupsX, threadGroupsY, 1);
        if(m_saveColourMap)
        {
            SaveTexture.SaveTextureToPNG(JFAResult, "Assets/Resources/TerrainGeneration/PlateColours.png", -1, -1);
        }
    }

    void GetHeightMap()
    {
        jumpFill.SetTexture(setHeightMapKernel, "PlateTracker", PlateTracker);
        jumpFill.SetTexture(setHeightMapKernel, "HeightMap", HeightMap);
        jumpFill.Dispatch(setHeightMapKernel, threadGroupsX, threadGroupsY, 1);
        if (m_saveHeightMap)
        {
            SaveTexture.SaveTextureToPNG(HeightMap, "Assets/Resources/TerrainGeneration/HeightMap.png", -1, -1);
        }
    }
    void TestWorldColours()
    {
        jumpFill.SetTexture(testWorldColoursKernel, "PlateTracker", PlateTracker);
        jumpFill.SetTexture(testWorldColoursKernel, "PlateResult", PlateResult);
        jumpFill.Dispatch(testWorldColoursKernel, threadGroupsX, threadGroupsY, 1);
    } 

    void SetTerrainData()
    {
        for (int i = 0; i < (int)m_heightMapResolution + 1; i++)
        {
            for (int j = 0; j < (int)m_heightMapResolution + 1; j++)
            {
                int j2 = j;
                int i2 = i;
                if (i == (int)m_heightMapResolution)
                {
                    i2--;
                }
                if (j == (int)m_heightMapResolution)
                {
                    j2--;
                }
                int k = j2 + (int)m_heightMapResolution * i2;

                terrainHeights[i, j] = points[k].elevation;
            }
        }
        tData = new TerrainData();
        tData.name = "TerrainData";
        tData.size = new Vector3(m_terrainResolution, m_heightScale, m_terrainResolution);        
        tData.heightmapResolution = (int)m_heightMapResolution;
        tData.alphamapResolution = (int)m_controlMapResolution;
        tData.baseMapResolution = (int)m_baseMapResolution;
        tData.SetDetailResolution((int)m_detailMapResolution, 16);
        tData.SetHeights(0, 0, terrainHeights);        

    }

    // Remap the elevation values to be between 0 and 1;
    float Remap(float value)
    {
        float low2 = 0;
        float high2 = 1;
        float low1 = m_minElevation;
        float high1 = m_maxElevation;
        return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
    }
}

