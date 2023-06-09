using UnityEngine;

[CreateAssetMenu(menuName = "Terrain Generator/Plate")]
public class PlateSO : ScriptableObject
{
    public Vector2Int coordinate;
    public float elevation;
    public Color color;
}