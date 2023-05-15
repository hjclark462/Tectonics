using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Plate
{
    // XY Coordinates Z elevation W type
    public Vector4[] points;
    public Vector4 centerPoint;    
    public int step;
    public Vector2 direction;   
}