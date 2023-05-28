using System.Collections;
using UnityEngine;


public class Tectonics : MonoBehaviour
{
    RenderTexture JFACalculation;
    RenderTexture JFAResult;
    RenderTexture PlateTracker;
    RenderTexture PlateResult;

    //public MeshRenderer mr;
    public ComputeShader jumpFill;
    ComputeBuffer plateBuffer;
    ComputeBuffer pointBuffer;
    ComputeBuffer colourBuffer;

    public int plateAmount = 16;
    Point[] plates;
    Point[] points;
    Vector4[] colours;

    int plateInitKernel;
    int jumpFillKernel;
    int jFAColoursKernel;
    int setPointDataKernel;
    int smoothElevationKernel;
    int setHeightMapKernel;

    int threadGroupsX;
    int threadGroupsY;

    public int mapWidth = 256;
    public int mapHeight = 256;

    public int smoothAmount = 1000;
    
    void Start()
    {
        InitTextures();
        plates = new Point[plateAmount];
        points = new Point[PlateTracker.width * PlateTracker.height];
        colours = new Vector4[plateAmount];

        for (int i = 0; i < plateAmount; i++)
        {
            Point p = new Point();
            p.pixel = new Vector2Int(Random.Range(0, JFACalculation.width - 1), Random.Range(0, JFACalculation.height - 1));
            p.plate = i;
            p.plateType = Random.Range(0, 1);
            p.direction = new Vector2Int(Random.Range(-1, 1), Random.Range(-1, 1));
            if (p.plateType == 1)
            {
                p.elevation = Random.Range(0.0f, 1000.0f);
            }
            else
            {
                p.elevation = Random.Range(-1000.0f, 0.0f);
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

        plateInitKernel = jumpFill.FindKernel("InitSeed");
        jumpFillKernel = jumpFill.FindKernel("JumpFill");
        setPointDataKernel = jumpFill.FindKernel("SetPointData");
        smoothElevationKernel = jumpFill.FindKernel("SmoothElevation");

        // Colour Buffers just for testing. Marked for removal
        jFAColoursKernel = jumpFill.FindKernel("TestJFAColours");
        setHeightMapKernel = jumpFill.FindKernel("TestElevationColours");


        InitPlates();
        JumpFloodAlgorithm();
        SetPointData();

        for (int i = 0; i < smoothAmount; i++)
        {
            SmoothElevation();
        }
        TestElevationColours();
        // TestJFAColours();
        //StartCoroutine(DebugPause(0.1f));        
        SaveTextureToFileUtility.SaveTextureToFile(PlateResult, "Assets/Textures/Test.png", -1, -1);
        Debug.Log("Should be saved");
    }

    IEnumerator DebugPause(float g)
    {

        yield return new WaitForSeconds(g);
        Debug.Log("breakpoint");


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

    void InitTextures()
    {
        JFACalculation = new RenderTexture(mapWidth, mapHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        JFACalculation.name = "JFACalculation";
        JFACalculation.enableRandomWrite = true;
        JFACalculation.Create();
        JFAResult = new RenderTexture(mapWidth, mapHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        JFAResult.name = "JFAResult";
        JFAResult.enableRandomWrite = true;
        JFAResult.Create();
        PlateTracker = new RenderTexture(mapWidth, mapHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        PlateTracker.name = "PlateTracker";
        PlateTracker.enableRandomWrite = true;
        PlateTracker.Create();
        PlateResult = new RenderTexture(mapWidth, mapHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        PlateResult.name = "PlateResult";
        PlateResult.enableRandomWrite = true;
        PlateResult.Create();

        threadGroupsX = Mathf.CeilToInt(JFACalculation.width / 8.0f);
        threadGroupsY = Mathf.CeilToInt(JFACalculation.height / 8.0f);
    }

    void InitPlates()
    {
        plateBuffer.SetData(plates);
        jumpFill.SetBuffer(plateInitKernel, "plates", plateBuffer);
        jumpFill.SetTexture(plateInitKernel, "JFACalculation", JFACalculation);
        jumpFill.SetTexture(plateInitKernel, "JFAResult", JFAResult);        
        jumpFill.SetInt("width", JFACalculation.width);
        jumpFill.SetInt("height", JFACalculation.height);
        jumpFill.Dispatch(plateInitKernel, plateAmount, 1, 1);
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

       // mr.sharedMaterial.SetTexture("_BaseMap", JFAResult);
    }

    void TestElevationColours()
    {
        jumpFill.SetTexture(setHeightMapKernel, "PlateTracker", PlateTracker);
        jumpFill.SetTexture(setHeightMapKernel, "PlateResult", PlateResult);
        jumpFill.Dispatch(setHeightMapKernel, threadGroupsX, threadGroupsY, 1);
    }
   
}

