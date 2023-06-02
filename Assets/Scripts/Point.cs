using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct Point
{    
    public Vector2Int position;
    public int plateType;
    public int plate;
    public int area;
    public float elevation;
    public Vector2Int direction;
}