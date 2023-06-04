using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class Plate : MonoBehaviour
{
    public Plate(Vector2 p)
    {
        points = new List<Point>();
        position = p;
    }

    public List<Point> points;
    public Vector2 position;
    public Vector2 speed = Vector2.zero;    
    public float mass = 0.0f;
    public float rotation = 0.0f;
    public float angularVelocity = 0.0f;
    public float inertia = 0.0f;
    public float convection = 10.0f;
    public float growth = 0.05f;
    public float area = 0.0f;
    public float height = 0.0f;

    void Recenter()
    {
        position = Vector2.zero;
        height = 0.0f;

        inertia = 0.0f;
        mass = 0.0f;
        area = 0.0f;

        foreach (Point s in points)
        {
            position += s.position;

            mass += s.mass;
            area += s.area;

            Vector2 pos = position - s.position;
            inertia += Mathf.Pow(Mathf.Sqrt(pos.x * pos.x + pos.y * pos.y), 2) * s.mass;
            height += s.height;
        }
        position /= points.Count;
        height /= points.Count;
    }
}
