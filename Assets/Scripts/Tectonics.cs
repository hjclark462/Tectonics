using System.Collections.Generic;
using TectonicEnums;
using UnityEngine;

[RequireComponent(typeof(Terrain), typeof(TerrainData))]
public class Tectonics : MonoBehaviour
{
    [SerializeField]
    public bool m_runtime = false;

    [SerializeField]
    public bool m_randomPlates = false;

    [SerializeField]
    public TerrainResolutions m_mapResolution;
    public TerrainResolutions m_baseMapResolution;
    public TerrainResolutions m_detailMapResolution;

    [SerializeField]
    public List<PlateSO> m_plateObjects;

    [SerializeField]
    public int m_heightScale = 5;

    [SerializeField]
    public int m_maxElevation = 1000;
    [SerializeField]
    public int m_minElevation = -1000;

    [SerializeField]
    public int m_mapLength;

    [SerializeField]
    public int smoothAmount = 100;

    [SerializeField]
    public int plateAmount;

    RenderTexture JFACalculation;
    RenderTexture JFAResult;
    RenderTexture PlateTracker;
    RenderTexture PlateResult;
    RenderTexture HeightMap;

    Terrain terrain;
    TerrainData tData;
    public Material material;

    //public MeshRenderer mr;
   public ComputeShader jumpFill;
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
            GenerateTerrain();
        }
    }

    private void Update()
    {

    }


    private void OnDisable()
    {
        plateBuffer.Release();
        pointBuffer.Release();
        colourBuffer.Release();
        plateBuffer = null;
        pointBuffer = null;
        colourBuffer = null;
    }

    public void GenerateTerrain()
    {
        InitTextures();
        InitBuffersKernel();
        InitPlateSeeds();
        JumpFloodAlgorithm();

        
        SetPointData();

        for (int i = 0; i < smoothAmount; i++)
        {
            SmoothElevation();
        }

        TestWorldColours();
        SetHeightMap();

        for (int i = 0; i < (int)m_mapResolution + 1; i++)
        {
            for (int j = 0; j < (int)m_mapResolution + 1; j++)
            {
                int j2 = j;
                int i2 = i;
                if (i == (int)m_mapResolution)
                {
                    i2--;
                }
                if (j == (int)m_mapResolution)
                {
                    j2--;
                }
                int k = j2 + (int)m_mapResolution * i2;

                terrainHeights[i, j] = RemapAndGreyscale(points[k].elevation);
            }
        }

        terrain = GetComponent<Terrain>();
        tData = terrain.terrainData;
        tData.size = new Vector3(m_mapLength, m_heightScale, m_mapLength);
        tData.heightmapResolution = (int)m_mapResolution;
        tData.baseMapResolution = (int)m_baseMapResolution;
        tData.SetDetailResolution((int)m_detailMapResolution, 16);
        tData.SetHeights(0, 0, terrainHeights);
        terrain.Flush();
        material.SetTexture("_BaseMap", PlateResult);
        terrain.materialTemplate = material;
        //SaveTextureToFileUtility.SaveTextureToFile(PlateResult, "Assets/Textures/PlateColours.png", -1, -1);
        // SaveTextureToFileUtility.SaveTextureToFile(HeightMap, "Assets/Textures/HeightMap.png", -1, -1);        
    }

    void InitTextures()
    {
        JFACalculation = new RenderTexture((int)m_mapResolution, (int)m_mapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        JFACalculation.name = "JFACalculation";
        JFACalculation.enableRandomWrite = true;
        JFACalculation.Create();
        JFAResult = new RenderTexture((int)m_mapResolution, (int)m_mapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        JFAResult.name = "JFAResult";
        JFAResult.enableRandomWrite = true;
        JFAResult.Create();
        PlateTracker = new RenderTexture((int)m_mapResolution, (int)m_mapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        PlateTracker.name = "PlateTracker";
        PlateTracker.enableRandomWrite = true;
        PlateTracker.Create();
        PlateResult = new RenderTexture((int)m_mapResolution, (int)m_mapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        PlateResult.name = "PlateResult";
        PlateResult.enableRandomWrite = true;
        PlateResult.Create();
        HeightMap = new RenderTexture((int)m_mapResolution, (int)m_mapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        HeightMap.name = "HeightMap";
        HeightMap.enableRandomWrite = true;
        HeightMap.Create();

        threadGroupsX = Mathf.CeilToInt(JFACalculation.width / 8.0f);
        threadGroupsY = Mathf.CeilToInt(JFACalculation.height / 8.0f);
    }

    void InitBuffersKernel()
    {
        plates = new Point[plateAmount];
        points = new Point[PlateTracker.width * PlateTracker.height];
        colours = new Vector4[plateAmount];
        terrainHeights = new float[(int)m_mapResolution + 1, (int)m_mapResolution + 1];

        for (int i = 0; i < plateAmount; i++)
        {
            Point p = new Point();
            p.pixel = new Vector2Int(Random.Range(0, JFACalculation.width - 1), Random.Range(0, JFACalculation.height - 1));
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
            p.direction = new Vector2Int(Random.Range(-1, 1), Random.Range(-1, 1));
            if (p.plateType == 1)
            {
                p.elevation = Random.Range(0.0f, m_maxElevation);
            }
            else
            {
                p.elevation = Random.Range(m_minElevation, 0.0f);
            }

            colours[i] = new Vector4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);

            plates[i] = p;
        }

        int pointSize = (sizeof(float) + sizeof(int) * 6);
        plateBuffer = new ComputeBuffer(plateAmount, pointSize);
        plateBuffer.SetData(plates);
        pointBuffer = new ComputeBuffer(points.Length + 1, pointSize, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        pointBuffer.SetData(points);

        // Colour Buffer just for testing. Marked for removal
        colourBuffer = new ComputeBuffer(plateAmount, sizeof(float) * 4);
        colourBuffer.SetData(colours);

        initPlateKernel = jumpFill.FindKernel("InitPlate");
        jumpFillKernel = jumpFill.FindKernel("JumpFill");
        setPointDataKernel = jumpFill.FindKernel("SetPointData");
        smoothElevationKernel = jumpFill.FindKernel("SmoothElevation");
        setHeightMapKernel = jumpFill.FindKernel("SetHeightMap");

        // Colour Buffers just for testing. Marked for removal
        jFAColoursKernel = jumpFill.FindKernel("TestJFAColours");
        testWorldColoursKernel = jumpFill.FindKernel("TestWorldColours");

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

    void SetPointData()
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

    void TestJFAColours()
    {
        colourBuffer.SetData(colours);
        jumpFill.SetTexture(jFAColoursKernel, "JFACalculation", JFACalculation);
        jumpFill.SetTexture(jFAColoursKernel, "JFAResult", JFAResult);
        jumpFill.Dispatch(jFAColoursKernel, threadGroupsX, threadGroupsY, 1);
    }

    void SetHeightMap()
    {        
        jumpFill.SetTexture(setHeightMapKernel, "PlateTracker", PlateTracker);
        jumpFill.SetTexture(setHeightMapKernel, "HeightMap", HeightMap);
        jumpFill.Dispatch(setHeightMapKernel, threadGroupsX, threadGroupsY, 1);
    }

    void TestWorldColours()
    {
        jumpFill.SetTexture(testWorldColoursKernel, "PlateTracker", PlateTracker);
        jumpFill.SetTexture(testWorldColoursKernel, "PlateResult", PlateResult);
        jumpFill.Dispatch(testWorldColoursKernel, threadGroupsX, threadGroupsY, 1);
    }

    float RemapAndGreyscale(float value)
    {
        float low2 = 0;
        float high2 = 1;
        float low1 = -1000;
        float high1 = 1000;
        return low2 + (value - low1) * (high2 - low2) / (high1 - low1);
    }
}

