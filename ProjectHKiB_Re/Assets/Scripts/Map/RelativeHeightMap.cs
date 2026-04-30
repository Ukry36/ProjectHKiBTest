using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
#endif

public class RelativeHeightMap: MonoBehaviour
{
    public Tilemap relativeHeightTilemap;
    public TilemapRenderer tilemapRenderer;
    public float baseZLevel;

    public void SetRelativeZlevel()
    {
        tilemapRenderer.material.SetFloat("_RelativeHeight", baseZLevel - GameManager.instance.player.GetInterface<IMovable>().ZLevel);
    }
}