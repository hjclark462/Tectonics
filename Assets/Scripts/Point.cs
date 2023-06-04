using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct Point
{    
    public Vector2Int position;    
    public int area;
    public Vector2Int direction; // More the velocity of the point
    public int alive;   
    public int colliding;
    public float mass; // Accumulated Mass
    public float thickness; // Thickness of Plate
    public float density; // Density of Mass in Plate
    public float height; // Height above Aesthenosphere (Buoyant)        
    public int plateNum;
}