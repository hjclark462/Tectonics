using UnityEngine;
using TectonicEnums;
using System.ComponentModel;

[CreateAssetMenu(menuName = "Tectonics/Plate")]
public class PlateSO : ScriptableObject
{
    public Vector2Int coordinate;
    public PlateType plateType;
    public float elevation;
    public Vector2Int direction;
}

namespace TectonicEnums
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
}