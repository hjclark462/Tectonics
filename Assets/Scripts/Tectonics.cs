using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Tectonics : MonoBehaviour
{
    RenderTexture JFACalculation;
    RenderTexture JFAResult;
    RenderTexture PlateTracker;
    RenderTexture PlateResult;
    RenderTexture HeightMap;

    GameObject terrain;
    TerrainData terrainData;

    public Material material;
    public MeshRenderer mr;
    public ComputeShader jumpFill;
    ComputeBuffer plateBuffer;
    ComputeBuffer pointBuffer;
    ComputeBuffer colourBuffer;

    public int plateAmount = 16;
    Point[] plates;
    Point[] points;
    Vector4[] colours;

    int initPlateKernel;
    int jumpFillKernel;
    int jFAColoursKernel;
    int setPointDataKernel;
    int smoothElevationKernel;
    int setHeightMapKernel;
    int testWorldColoursKernel;

    int threadGroupsX;
    int threadGroupsY;

    public int mapWidth = 256;
    public int mapHeight = 256;

    public int smoothAmount = 10;

    public float heightScale = 5;

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

        initPlateKernel = jumpFill.FindKernel("InitPlate");
        jumpFillKernel = jumpFill.FindKernel("JumpFill");
        setPointDataKernel = jumpFill.FindKernel("SetPointData");
        smoothElevationKernel = jumpFill.FindKernel("SmoothElevation");
        setHeightMapKernel = jumpFill.FindKernel("SetHeightMap");

        // Colour Buffers just for testing. Marked for removal
        jFAColoursKernel = jumpFill.FindKernel("TestJFAColours");
        testWorldColoursKernel = jumpFill.FindKernel("TestWorldColours");


        InitPlates();
        JumpFloodAlgorithm();
        SetPointData();

        for (int i = 0; i < smoothAmount; i++)
        {
            SmoothElevation();
        }

        TestWorldColours();
        SetHeightMap();

        CreateTerrain();
        //SaveTextureToFileUtility.SaveTextureToFile(PlateResult, "Assets/Textures/PlateColours.png", -1, -1);
        // SaveTextureToFileUtility.SaveTextureToFile(HeightMap, "Assets/Textures/HeightMap.png", -1, -1);        
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
        HeightMap = new RenderTexture(mapWidth, mapHeight, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        HeightMap.name = "HeightMap";
        HeightMap.enableRandomWrite = true;
        HeightMap.Create();

        threadGroupsX = Mathf.CeilToInt(JFACalculation.width / 8.0f);
        threadGroupsY = Mathf.CeilToInt(JFACalculation.height / 8.0f);
    }

    void InitPlates()
    {
        plateBuffer.SetData(plates);
        jumpFill.SetBuffer(initPlateKernel, "plates", plateBuffer);
        jumpFill.SetTexture(initPlateKernel, "JFACalculation", JFACalculation);
        jumpFill.SetTexture(initPlateKernel, "JFAResult", JFAResult);
        jumpFill.SetInt("width", JFACalculation.width);
        jumpFill.SetInt("height", JFACalculation.height);
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

        mr.sharedMaterial.SetTexture("_BaseMap", JFAResult);
    }

    void SetHeightMap()
    {
        jumpFill.SetTexture(setHeightMapKernel, "PlateTracker", PlateTracker);
        jumpFill.SetTexture(setHeightMapKernel, "HeightMap", HeightMap);
        jumpFill.Dispatch(setHeightMapKernel, threadGroupsX, threadGroupsY, 1);
        mr.sharedMaterial.SetFloat("_Scale", heightScale);
        mr.sharedMaterial.SetTexture("_HeightMap", HeightMap);
    }

    void TestWorldColours()
    {
        jumpFill.SetTexture(testWorldColoursKernel, "PlateTracker", PlateTracker);
        jumpFill.SetTexture(testWorldColoursKernel, "PlateResult", PlateResult);
        jumpFill.Dispatch(testWorldColoursKernel, threadGroupsX, threadGroupsY, 1);
        mr.sharedMaterial.SetFloat("_Scale", heightScale);
        mr.sharedMaterial.SetTexture("_BaseMap", PlateResult);
    }

    void CreateTerrain()
    {
        Texture2D hMap = new Texture2D(mapWidth, mapHeight, TextureFormat.RGB24, false);
        RenderTexture.active = HeightMap;
        hMap.ReadPixels(new Rect(0, 0, hMap.width, hMap.height), 0, 0);
        hMap.Apply();
        
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        //Bottom left section of the map, other sections are similar
        for (int i = 0; i < 250; i++)
        {
            for (int j = 0; j < 250; j++)
            {
                //Add each new vertex in the plane
                verts.Add(new Vector3(i, hMap.GetPixel(i, j).grayscale * 100, j));
                //Skip if a new square on the plane hasn't been formed
                if (i == 0 || j == 0) continue;
                //Adds the index of the three vertices in order to make up each of the two tris
                tris.Add(250 * i + j); //Top right
                tris.Add(250 * i + j - 1); //Bottom right
                tris.Add(250 * (i - 1) + j - 1); //Bottom left - First triangle
                tris.Add(250 * (i - 1) + j - 1); //Bottom left 
                tris.Add(250 * (i - 1) + j); //Top left
                tris.Add(250 * i + j); //Top right - Second triangle
            }
        }

        Vector2[] uvs = new Vector2[verts.Count];
        for (var i = 0; i < uvs.Length; i++) //Give UV coords X,Z world coords
            uvs[i] = new Vector2(verts[i].x, verts[i].z);

        GameObject plane = new GameObject("ProcPlane"); //Create GO and add necessary components
        plane.AddComponent<MeshFilter>();
        plane.AddComponent<MeshRenderer>();
        Mesh procMesh = new Mesh();
        procMesh.vertices = verts.ToArray(); //Assign verts, uvs, and tris to the mesh
        procMesh.uv = uvs;
        procMesh.triangles = tris.ToArray();
        procMesh.RecalculateNormals(); //Determines which way the triangles are facing
        plane.GetComponent<MeshFilter>().mesh = procMesh; //Assign Mesh object to MeshFilter
        MeshRenderer mrp = plane.GetComponent<MeshRenderer>();
        mrp.material = material;
    }

}

