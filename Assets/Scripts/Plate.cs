using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Point
{
    // XY Coordinates Z elevation W plateNumber
    public Vector4 points;
    public int plateType;
    public float distance;
    public Vector2Int direction;   
}