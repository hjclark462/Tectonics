using System.Collections.Generic;
using UnityEngine;
using TectonicValues;
using System.Runtime.InteropServices;
using UnityEngine.UIElements;

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

    public Point[] Update(Point[] allPoints, float[,] heatmap, int numPlates, int size)
    {
        Vector2 acc = Vector2.zero;
        float torque = 0/0f;

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
                curPos += size * Values.CR * new Vector2(Mathf.Cos(j / numPlates * 2.0f * Mathf.PI), Mathf.Sin(j / numPlates * 2.0f * Mathf.PI));

                if(curPos.x >= size || curPos.x < 0 || curPos.y >= size || curPos.y< 0) continue;

                Point next = allPoints[Values.PointIndex(curPos, size)];
                if (cur.plateNum == next.plateNum) continue;

                if (cur.density > next.density && next.colliding == 1)
                {
                    float mdiff = cur.height * cur.density * cur.area;
                    float hdiff = cur.height;

                    next.thickness += hdiff;
                    next.mass += mdiff;
                    next = Values.Buoyancy(next);

                    next.colliding = 1;
                    cur.alive = 0;
                }
                allPoints[Values.PointIndex(curPos, size)] = next;
            }
            points[i] = cur;
        }
        for(int i = 0; i < allPoints.Length; i++) 
        {
            if (allPoints[i].colliding == 0) continue;
            Point cur = allPoints[i];

            int numPlate2 = numPlates * 2;
            for(int j = 0; j<numPlate2; j++) 
            {
                Vector2 curPos = cur.position;
                curPos += size * Values.R * new Vector2(Mathf.Cos(j / numPlates * 2.0f * Mathf.PI), Mathf.Sin(j / numPlates * 2.0f * Mathf.PI));

                if (curPos.x >= size || curPos.x < 0 || curPos.y >= size || curPos.y < 0) continue;

                Point next = allPoints[Values.PointIndex(curPos, size)];
                if (cur.plateNum == next.plateNum) continue;

                float hdiff = cur.height - next.height;

                hdiff -= 0.01f;
                if (hdiff < 0) continue;

                float mdiff = hdiff * cur.density * cur.area;

                float trate = 0.2f;

                next.thickness += 0.5f * trate * hdiff;
                cur.thickness -= 0.5f * trate * hdiff;
                next.mass += 0.5f * trate * mdiff;
                cur.mass -= 0.5f * trate * mdiff;

                next = Values.Buoyancy(next);
                cur = Values.Buoyancy(cur);
                allPoints[Values.PointIndex(curPos, size)] = next;
            }
            cur.colliding = 0;
            allPoints[i] = cur;
        }

        for(int i = 0;i< points.Count;i++)
        {
            if (points[i].alive == 0) continue;

            Vector2Int ip = points[i].position;
            float nd = heatmap[ip.y, ip.x];

            float G = growth * (1.0f - nd) * (1.0f - nd - points[i].density * points[i].thickness);
            if(G < 0.0f) G *= 0.05f;

            float D = Values.Langmuir(3.0f, 1.0f - nd);

            Point p = points[i];
            p.mass += p.area * G * D;
            p.thickness = p.thickness + G;

            p.density = p.mass/(p.area*p.thickness);
            p.height = p.thickness*(1.0f-p.density);
            points[i] = p;
        }

        for(int i = 0;  i< points.Count;i++) 
        {
            Vector2 f = Values.Force(points[i].position, heatmap, size);
            Vector2 direction = points[i].position - position;

            acc -= convection * f;
            torque -= convection * direction.magnitude * f.magnitude * Mathf.Sin(Values.Angle(f) - Values.Angle(direction));
        }

        speed += Values.DT * acc / mass;
        angularVelocity += 1E4f * torque / inertia;
        position += Values.DT *speed;
        rotation += Values.DT * angularVelocity ;

        for(int i = 0;i < points.Count;i++) 
        {
            Point p = points[i];
            Vector2 direction = p.position - (position - Values.DT * speed);
            float angle = Values.Angle(direction) - (rotation - Values.DT * angularVelocity);

            Vector2 dtmp = (position + direction.magnitude * new Vector2(Mathf.Cos(rotation + angle), Mathf.Sin(rotation + angle))) - p.position;
            p.direction = new Vector2Int((int)dtmp.x, (int)dtmp.y);
            Vector2 ptmp = position + direction.magnitude * new Vector2(Mathf.Cos(rotation + angle), Mathf.Sin(rotation + angle)); 
            p.position = new Vector2Int((int)ptmp.x,(int)ptmp.y);   
        }
        return allPoints;
    }
}
