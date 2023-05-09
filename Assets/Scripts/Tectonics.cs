using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tectonics : MonoBehaviour
{
    RenderTexture heightMap;
    ComputeShader jumpFill;
    ComputeBuffer plateBuffer;

    int plateAmount = 5;
    Plate[] plates;

    int kernal;

    // Start is called before the first frame update
    void Start()
    {
        plates = new Plate[plateAmount];
        for (int i = 0; i < plateAmount; i++)
        {
            Plate p = new Plate();
            p.type = Random.Range(0, 1);
            p.centerPoint = new Point();
            p.centerPoint.position = new Vector2Int(Random.Range(0, heightMap.width), Random.Range(0, heightMap.height));
            if (p.type == 1)
            {
                p.centerPoint.elevation = Random.Range(0.0f, 1.0f);
            }
            else
            {
                p.centerPoint.elevation = Random.Range(-1.0f, 0.0f);
            }
            plates[i] = p;
        }
        int plateSize = sizeof(int) * (heightMap.width * heightMap.height * 2 + 4) + sizeof(float) * (heightMap.width * heightMap.height + 2);
        plateBuffer = new ComputeBuffer(plateAmount, plateSize);
        plateBuffer.SetData(plates);
        kernal = jumpFill.FindKernel("InitSeed");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
