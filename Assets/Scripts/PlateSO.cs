using UnityEngine;
using TectonicValues;
using System.ComponentModel;

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

        public static int PointIndex(Vector2 p, int size)
        {
            return (int)p.y * size + (int)p.x;
        }
    }
}