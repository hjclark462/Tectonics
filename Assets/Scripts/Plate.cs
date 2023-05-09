using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Plate
{
    public Point[] plateSpace;
    public Point centerPoint;

    public int spreadSpeed;

    public Vector2 direction;
    public int type;
}

public struct Point
{
   public Vector2Int position;
   public float elevation;
}