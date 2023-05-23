using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class Tectonics : MonoBehaviour
{
    RenderTexture JFACalculation;
    RenderTexture JFAResult;
    RenderTexture PlateTracker;
    RenderTexture PlateResult;

    public MeshRenderer mr;
    public ComputeShader jumpFill;
    ComputeBuffer plateBuffer;
    ComputeBuffer pointBuffer;
    ComputeBuffer colourBuffer;

    int plateAmount = 10;
    Point[] plates;
    Point[] points;
    Vector4[] colours;

    int plateInitKernel;
    int jumpFillKernel;
    int coloursKernel;

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
            p.distance = 0.0f;
            p.direction = new Vector2Int(Random.Range(-1, 1), Random.Range(-1, 1));
            if (p.plateType == 1)
            {
                p.elevation = Random.Range(0.0f, 1.0f);
            }
            else
            {
                p.elevation = Random.Range(-1.0f, 0.0f);
            }

            colours[i] = new Vector4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);

            plates[i] = p;
        }

        int pointSize = (sizeof(float) * 2 + sizeof(int) * 6);
        plateBuffer = new ComputeBuffer(plateAmount, pointSize);
        plateBuffer.SetData(plates);
        pointBuffer = new ComputeBuffer(points.Length + 1, pointSize);
        pointBuffer.SetData(points);
        colourBuffer = new ComputeBuffer(plateAmount, sizeof(float) * 4);
        colourBuffer.SetData(colours);

        plateInitKernel = jumpFill.FindKernel("InitSeed");
        coloursKernel = jumpFill.FindKernel("TestColours");
        jumpFillKernel = jumpFill.FindKernel("JumpFill");

        TestColours();
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
        JFACalculation = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        JFACalculation.name = "JFACalculation";
        JFACalculation.enableRandomWrite = true;
        JFACalculation.Create();
        JFAResult = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        JFAResult.name = "JFAResult";
        JFAResult.enableRandomWrite = true;
        JFAResult.Create();
        PlateTracker = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        PlateTracker.name = "PlateTracker";
        PlateTracker.enableRandomWrite = true;
        PlateTracker.Create(); 
        PlateResult = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        PlateResult.name = "PlateResult";
        PlateResult.enableRandomWrite = true;
        PlateResult.Create();
    }

    void TestColours()
    {
        plateBuffer.SetData(plates);
        jumpFill.SetBuffer(plateInitKernel, "plates", plateBuffer);
        jumpFill.SetBuffer(plateInitKernel, "points", pointBuffer);
        jumpFill.SetTexture(plateInitKernel, "Source", JFACalculation);
        jumpFill.SetTexture(plateInitKernel, "Result", JFAResult);
        jumpFill.SetInt("width", JFACalculation.width);
        jumpFill.SetInt("height", JFACalculation.height);
        jumpFill.Dispatch(plateInitKernel, plateAmount, 1, 1);


        int stepAmount = (int)Mathf.Log(Mathf.Max(JFACalculation.width, JFACalculation.height), 2);
        int threadGroupsX = Mathf.CeilToInt(JFACalculation.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(JFACalculation.height / 8.0f);

        for (int i = 0; i < stepAmount; i++)
        {
            int step = (int)Mathf.Pow(2, stepAmount - i - 1);
            jumpFill.SetInt("step", step);
            jumpFill.SetTexture(jumpFillKernel, "Source", JFACalculation);
            jumpFill.SetTexture(jumpFillKernel, "Result", JFAResult);
            jumpFill.Dispatch(jumpFillKernel, threadGroupsX, threadGroupsY, 1);
            Graphics.Blit(JFAResult, JFACalculation);
        }

        jumpFill.SetBuffer(coloursKernel, "colours", colourBuffer);
        jumpFill.SetTexture(coloursKernel, "Source", JFACalculation);
        jumpFill.SetTexture(coloursKernel, "Result", JFAResult);
        jumpFill.Dispatch(coloursKernel, threadGroupsX, threadGroupsY, 1);

        mr.sharedMaterial.SetTexture("_BaseMap", JFAResult);
    }
}
