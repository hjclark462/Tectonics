using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tectonics : MonoBehaviour
{
    float[,] heightMapData;

    Texture heightMap;
    ComputeShader jumpFill;
    ComputeBuffer plateBuffer;

    public int plateAmount;

    Plate[] plates;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
