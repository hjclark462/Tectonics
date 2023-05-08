using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Plate
{
    Point[] plateSpace;
    Point centerPoint;

    Vector2 direction;
    int type;
}

public struct Point
{
    Vector2Int position;
    float elevation;
}