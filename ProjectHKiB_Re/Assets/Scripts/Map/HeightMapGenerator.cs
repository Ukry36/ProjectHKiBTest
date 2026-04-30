using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class HeightMapGenerator: MonoBehaviour
{
    public Tilemap heightTilemap;
    public SerializedDictionary<TileBase, float> zValueMap;
    public GameObject RelativeHeightMapPrefab;
    public Tilemap referenceTilemap;

#if UNITY_EDITOR
    [NaughtyAttributes.Button]
    public void GenerateDictionary()
    {
        zValueMap = new();
        string[] guids = AssetDatabase.FindAssets("t:HeightTile");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            HeightTile tile = AssetDatabase.LoadAssetAtPath<HeightTile>(path);
            zValueMap.Add(tile, tile.zLevel);
        }
    }
#endif

    public void Start()
    {
        Dictionary<float, RelativeHeightMap> childRelativeHeightMaps = new();
        //might be possible using tilemap.GetSprite to get one tile's detailed heightmap..
        BoundsInt bounds = heightTilemap.cellBounds;

        for (int x = bounds.min.x; x < bounds.max.x; x++)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                TileBase tile = heightTilemap.GetTile(Vector3Int.right * x + Vector3Int.up * y);
                TileBase refTile = referenceTilemap.GetTile(Vector3Int.right * x + Vector3Int.up * y);
                
                if (tile != null && zValueMap.TryGetValue(tile, out float z))
                {
                    if (!childRelativeHeightMaps.ContainsKey(z))
                    {
                        childRelativeHeightMaps[z] = Instantiate(RelativeHeightMapPrefab, transform.position, Quaternion.identity, transform).GetComponent<RelativeHeightMap>();
                        childRelativeHeightMaps[z].baseZLevel = z;
                    }
                    childRelativeHeightMaps[z].relativeHeightTilemap.SetTile(Vector3Int.right * x + Vector3Int.up * y, refTile);
                }
            }
        }
        Destroy(heightTilemap);
        Destroy(this);
    }
}