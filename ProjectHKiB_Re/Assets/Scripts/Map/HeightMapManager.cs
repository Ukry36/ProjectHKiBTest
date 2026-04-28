using AYellowpaper.SerializedCollections;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class HeightMapManager: MonoBehaviour
{
    public Tilemap heightTilemap;
    public SerializedDictionary<TileBase, int> zValueMap;

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
        
    }

    public int GetZLevel(Vector3 worldPosition) 
    {
        Vector3Int cellPos = heightTilemap.WorldToCell(worldPosition);
        TileBase tile = heightTilemap.GetTile(cellPos);
    
        if (tile != null && zValueMap.TryGetValue(tile, out int z)) 
        {
            return z;
        }
        return 0;
    }
}