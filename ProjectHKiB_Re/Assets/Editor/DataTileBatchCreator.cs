using UnityEngine;
using UnityEditor;
using System.IO;

public class DataTileBatchCreator : EditorWindow
{
    private string folderPath = "Assets/Map/Public/Heightmap Assets";
    private int startZ = 0;
    private int endZ = 10;
    private Sprite baseSprite;
    private Gradient heightGradient1 = new();

    [MenuItem("Tools/DataTile Batch Creator")]
    public static void ShowWindow()
    {
        GetWindow<DataTileBatchCreator>("Tile Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Batch Create DataTiles", EditorStyles.boldLabel);

        folderPath = EditorGUILayout.TextField("Save Path", folderPath);
        baseSprite = (Sprite)EditorGUILayout.ObjectField("Base Sprite", baseSprite, typeof(Sprite), false);
        
        startZ = EditorGUILayout.IntField("Start Z Level", startZ);
        endZ = EditorGUILayout.IntField("End Z Level", endZ);
        heightGradient1 = EditorGUILayout.GradientField("Gradient", heightGradient1);

        if (GUILayout.Button("Generate Tiles"))
        {
            CreateTiles();
        }
    }

    private void CreateTiles()
    {
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        for (int z = startZ; z <= endZ; z++)
        {
            HeightTile newTile = ScriptableObject.CreateInstance<HeightTile>();
            
            newTile.zLevel = z;
            float t = Mathf.InverseLerp(startZ, endZ, z);
            newTile.color = heightGradient1.Evaluate(t);
            newTile.sprite = baseSprite;
            newTile.name = $"Tile_Z{z}";

            string fullPath = Path.Combine(folderPath, $"{newTile.name}.asset");
            AssetDatabase.CreateAsset(newTile, fullPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"{endZ - startZ + 1}개의 타일이 {folderPath}에 생성되었습니다.");
    }
}