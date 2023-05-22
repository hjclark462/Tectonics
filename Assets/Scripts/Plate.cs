using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct Point
{
    // XY Coordinates Z elevation W plateNumber
    public Vector2Int pixel;
    public float elevation;
    public int plate;
    public int plateType;
    public float distance;
    public Vector2Int direction;
}