using System.Collections.Generic;
using UnityEngine;
using AYellowpaper.SerializedCollections;
using UnityEngine.Tilemaps;
using System.Collections;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class HeightMapManager: MonoBehaviour
{
    public RenderTexture renderTexture;
    public GameObject DynamicHeightMapPrefab;
    public SerializedDictionary<TileBase, float> zValueMap;

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
        HeightMapGenerator[] generators = FindObjectsOfType<HeightMapGenerator>();
        for (int i = 0; i < generators.Length; i++)
            generators[i].Initialize(DynamicHeightMapPrefab, zValueMap);
    }

    public void Update()
    {
        Shader.SetGlobalTexture("_RelativeHeight", renderTexture);
    }
}