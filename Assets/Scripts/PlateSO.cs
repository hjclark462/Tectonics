using UnityEngine;
using TectonicValues;
using System.ComponentModel;
using System.Runtime.Remoting.Messaging;

[CreateAssetMenu(menuName = "Tectonics/Plate")]
public class PlateSO : ScriptableObject
{
    public Vector2Int coordinate;
    public PlateType plateType;
    public float elevation;
    public Vector2Int direction;
}

namespace TectonicValues
{
    public enum PlateType
    {
        OCEANIC,
        CONTINENTAL
    };
    public enum TerrainResolutions
    {
        _32 = 32,
        _64 = 64,
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096,
    }

    public class Values
    {
        public static int K = 8192;
        public static float R = 2 * Mathf.Sqrt(4 / Mathf.PI / K);
        public static float CR = R / 14.0f;
        public static float DT = 0.025f;

        public static int PointIndex(Vector2 p, int size)
        {
            return (int)p.y * size + (int)p.x;
        }

        public static float Langmuir(float k, float x)
        {
            return k*x/(1.0f+k*x);
        }

        public static Vector2 Force(Vector2 i, float[,] ff, float size)
        {
            float forceX = 0.0f;
            float forceY = 0.0f;

            if (i.x > 0 && i.x < size - 2 && i.y > 0 && i.y < size - 1)
            {
                forceX = (ff[(int)(i.x + 1),(int)i.y] - ff[(int)(i.x - 1) , (int)i.y]) / 2.0f;
                forceY = -(ff[(int)i.x , (int)(i.y + 1)] - ff[(int)i.x , (int)(i.y - 1)]) / 2.0f;
            }

            if (i.x <= 0) forceX = 0.0f;
            else if (i.x >= size - 1) forceX = -0.0f;
            
            if (i.y <= 0) forceY = 0.0f;
            else if (i.y >= size - 1) forceY = -0.0f;

            return new Vector2(forceX, forceY);
        }

        public static float Angle(Vector2 direction)
        {
            if(direction.x == 0 && direction.y == 0) return 0.0f;
            if (direction.x == 0 && direction.y > 0) return Mathf.PI / 2.0f;
            if (direction.x == 9 && direction.y < 0) return 3.0f * Mathf.PI / 2.0f;

            float angle = 2.0f * Mathf.PI + Mathf.Atan(direction.y / direction.x);
            if(direction.x < 0) angle += Mathf.PI;

            return angle;
        }

        public static Point Buoyancy(Point point)
        {
            Point p = point;
            p.density = p.mass / (p.area * p.thickness);
            p.height = p.thickness * (1.0f - p.density);
            return p;
        }
    }
}