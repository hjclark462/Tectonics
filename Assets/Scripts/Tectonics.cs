using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class Tectonics : MonoBehaviour
{
    RenderTexture test1;
    RenderTexture test2;
    public MeshRenderer mr;
    public ComputeShader jumpFill;
    ComputeBuffer plateBuffer;
    ComputeBuffer colourBuffer;

    int plateAmount = 16;
    List<Point> points;
    Vector4[] colours;

    int plateInitKernel;
    int jumpFillKernel;
    int coloursKernel;

    void Start()
    {
        InitTextures();
        points = new List<Point>();
        colours = new Vector4[plateAmount];
        for (int i = 0; i < plateAmount; i++)
        {
            colours[i] = Vector4.one;
        }
        for (int i = 0; i < plateAmount; i++)
        {
            Point p = new Point();
            p.plateType = Random.Range(0, 1);
            p.direction = new Vector2Int(Random.Range(-2, 2), Random.Range(-2, 2));
            p.points = new Vector4(Random.Range(0, test1.width), Random.Range(0, test1.height), 0, i);
            if (p.plateType == 1)
            {
                p.elevation = Random.Range(0.0f, 1.0f);
            }
            else
            {
                p.elevation = Random.Range(-1.0f, 0.0f);
            }
            colours[i] = new Vector4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
            points.Add(p);
        }

        int pointArraySize = (sizeof(float) * 5 + sizeof(int) * 3);
        plateBuffer = new ComputeBuffer(plateAmount, pointArraySize, ComputeBufferType.Append);
        plateBuffer.SetData(points);
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
        colourBuffer.Release();
        plateBuffer = null;
        colourBuffer = null;
    }

    void InitTextures()
    {
        test1 = new RenderTexture(256, 256, 24);
        test1.name = "test1";
        test1.enableRandomWrite = true;
        test1.Create();
        test2 = new RenderTexture(256, 256, 24);
        test2.name = "test2";
        test2.enableRandomWrite = true;
        test2.Create();

        RenderTexture rt = UnityEngine.RenderTexture.active;
        UnityEngine.RenderTexture.active = test1;
        GL.Clear(true, true, Color.clear);
        UnityEngine.RenderTexture.active = rt;
    }

    void TestColours()
    {
        plateBuffer.SetData(points);
        jumpFill.SetBuffer(plateInitKernel, "points", plateBuffer);
        jumpFill.SetTexture(plateInitKernel, "Source", test1);
        jumpFill.SetTexture(plateInitKernel, "Result", test2);
        jumpFill.SetInt("width", test1.width);
        jumpFill.SetInt("height", test1.height);



        jumpFill.Dispatch(plateInitKernel, plateAmount, 1, 1);


        int stepAmount = (int)Mathf.Log(Mathf.Max(test1.width, test1.height), 2);

        int threadGroupsX = Mathf.CeilToInt(test1.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(test1.height / 8.0f);

        for (int i = 0; i < stepAmount; i++)
        {
            int step = (int)Mathf.Pow(2, stepAmount - i - 1);
            jumpFill.SetInt("step", step);
            jumpFill.SetTexture(jumpFillKernel, "Source", test1);
            jumpFill.SetTexture(jumpFillKernel, "Result", test2);


            jumpFill.Dispatch(jumpFillKernel, threadGroupsX, threadGroupsY, 1);
            Graphics.Blit(test2, test1);

        }

        jumpFill.SetBuffer(coloursKernel, "colours", colourBuffer);
        jumpFill.SetTexture(coloursKernel, "Source", test1);
        jumpFill.SetTexture(coloursKernel, "Result", test2);

        jumpFill.Dispatch(coloursKernel, threadGroupsX, threadGroupsY, 1);


        Graphics.Blit(test2, test1);
        mr.sharedMaterial.SetTexture("_BaseMap", test1);
    }
}
