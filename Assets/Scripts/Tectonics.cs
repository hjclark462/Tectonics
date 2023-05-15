using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class Tectonics : MonoBehaviour
{
    public RenderTexture test1;
    public RenderTexture test2;
    public ComputeShader jumpFill;
    ComputeBuffer plateBuffer;
    ComputeBuffer colourBuffer;

    int plateAmount = 4;
    Plate[] plates;
    Vector4[] colours;

    int plateInit;
    int coloursKernal;

    // Start is called before the first frame update
    void Start()
    {
        InitTextures();    
        plates = new Plate[plateAmount];
        colours = new Vector4[plateAmount];
        for (int i = 0; i < plateAmount; i++)
        {
            Plate p = new Plate();
            int type = Random.Range(0, 1);
            p.centerPoint = new Vector4(Random.Range(0, test1.width), Random.Range(0, test1.height), 0, type);
            if (type == 1)
            {
                p.centerPoint.z = Random.Range(0.0f, 1.0f);
            }
            else
            {
                p.centerPoint.z = Random.Range(-1.0f, 0.0f);
            }
            colours[i] = new Vector4(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1);
            plates[i] = p;
        }        
        
        plateBuffer = new ComputeBuffer(plateAmount, 2048);
        plateBuffer.SetData(plates);
        colourBuffer = new ComputeBuffer(plateAmount, sizeof(float)*3*plateAmount);
        colourBuffer.SetData(colours);
        plateInit = jumpFill.FindKernel("InitSeed");
        coloursKernal = jumpFill.FindKernel("TestColours");
        TestColours();
    }

    private void OnDisable()
    {
        Debug.Log("Dest");
        plateBuffer.Release();
        colourBuffer.Release();        
    }

    void InitTextures()
    {
        test1 = new RenderTexture(255, 255, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        test1.enableRandomWrite = true;
        test1.Create();
        test2 = new RenderTexture(255, 255, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        test2.enableRandomWrite = true;
        test2.Create();

        GL.Clear(true, true, Color.clear);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void TestColours() 
    {
        plateBuffer.SetData(plates);
        jumpFill.SetBuffer(plateInit, "plates", plateBuffer);
        jumpFill.SetTexture(plateInit, "Source", test1);
        jumpFill.SetInt("width", test1.width);
        jumpFill.SetInt("height", test1.height);
        jumpFill.Dispatch(plateInit, plateAmount, 1, 1);

        int threadGroupsX = Mathf.CeilToInt(test1.width / 8.0f);
        int threadGroupsY = Mathf.CeilToInt(test1.height / 8.0f);

        jumpFill.SetBuffer(coloursKernal, "colours", colourBuffer);
        jumpFill.SetTexture(coloursKernal, "Source", test1);
        jumpFill.SetTexture(coloursKernal, "Result", test2);
        jumpFill.Dispatch(coloursKernal, threadGroupsX, threadGroupsY, 1);

        Graphics.Blit(test1, test2);
    }
}
