using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class Tectonics : MonoBehaviour
{
    public RenderTexture test1A;
    public RenderTexture test2A;
    RenderTexture test1;
    RenderTexture test2;
    public MeshRenderer mr;
    public ComputeShader jumpFill;
    ComputeBuffer plateBuffer;
    ComputeBuffer colourBuffer;

    int plateAmount = 4;
    List<Point> points;
    Vector4[] colours;

    int plateInitKernel;
    int coloursKernal;
        
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
                p.points.z = Random.Range(0.0f, 1.0f);
            }
            else
            {
                p.points.z = Random.Range(-1.0f, 0.0f);
            }
            colours[i] = new Vector4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
            points.Add(p);
        }        
        
        int pointArraySize = (sizeof(float) * 5 + sizeof(int) *3);
        plateBuffer = new ComputeBuffer(plateAmount, pointArraySize);
        plateBuffer.SetData(points);
        colourBuffer = new ComputeBuffer(plateAmount, sizeof(float)*4);
        colourBuffer.SetData(colours);
        
        plateInitKernel = jumpFill.FindKernel("InitSeed");
        coloursKernal = jumpFill.FindKernel("TestColours");
    }

    private void Update()
    {
        TestColours();
        
    }

    private void OnDisable()
    {
        Debug.Log("Dest");
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
        /*plateBuffer.SetData(points);
        jumpFill.SetBuffer(plateInitKernel, "points", plateBuffer);
        jumpFill.SetTexture(plateInitKernel, "Source", test1);
        jumpFill.SetTexture(plateInitKernel, "Result", test2);
        jumpFill.SetInt("width", test1.width);
        jumpFill.SetInt("height", test1.height);
        jumpFill.Dispatch(plateInitKernel, plateAmount, 1, 1);*/

        int threadGroupsX = Mathf.CeilToInt(test1.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(test1.height / 8.0f);

        jumpFill.SetBuffer(coloursKernal, "colours", colourBuffer);
        jumpFill.SetTexture(coloursKernal, "Source", test1, 0, UnityEngine.Rendering.RenderTextureSubElement.Color);
        jumpFill.SetTexture(coloursKernal, "Result", test2, 0, UnityEngine.Rendering.RenderTextureSubElement.Color);
        jumpFill.Dispatch(coloursKernal, threadGroupsX, threadGroupsY, 1);

        test1A = test1;
        test2A = test2;
        mr.sharedMaterial.SetTexture("_BaseMap", test1);
    }
}
