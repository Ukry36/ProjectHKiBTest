using System;
using UnityEngine;
[CreateAssetMenu(fileName = "Height Tile Setting", menuName = "Tiles/Height Tile Setting")]
public class HeightTileSetting: ScriptableObject
{
    public Gradient heightGradient1;
    public int range1;
    public Gradient heightGradient2;
    public Vector2 range2;

}