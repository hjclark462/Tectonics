using System.Collections.Generic;
using UnityEngine;
using TectonicValues;
using Unity.VisualScripting;

public class Plate
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

    private void Update(Point[] allPoints, float[,] heatmap, int numPlates, int size)
    {
        Vector2 acc = Vector2.zero;
        float torque;

        for(int i = 0; i < points.Count; i++)
        {
            Point cur = points[i];
            
            Vector2 curPos = cur.position;
            if (curPos.x >= size || curPos.x < 0 || curPos.y >= size || curPos.y < 0)
            {
                cur.alive = 0;
                continue;
            }

            for(int j = 0; j < numPlates; j++)
            {
                curPos += size * Values.CR * new Vector2(Mathf.Cos((float)j / (float)numPlates * 2.0f * Mathf.PI), Mathf.Sin((float)j / (float)numPlates * 2.0f * Mathf.PI));

                if(curPos.x >= size || curPos.x < 0 || curPos.y >= size || curPos.y< 0) continue;

                Point next = points[Values.PointIndex(curPos, size)];
                if (cur.plateNum == next.plateNum) continue;

                if (cur.density > next.density && next.colliding == 1)
                {
                    float mdiff = cur.height * cur.density * cur.area;
                    float hdiff = cur.height;

                    next.thickness += hdiff;
                    next.mass += mdiff;
                    //next.buoyancy();

                    next.colliding = 1;
                    cur.alive = 0;
                }

            }




            points[i] = cur;
        }
    }
}
