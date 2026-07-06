using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    private bool clearCurrentScene = false;
    private SceneInstance currentLoadedScene;
    public MapDataSO CurrentMapData { get; private set; }
    public MapLocalManager localManager;

    public MapDataSO temp;
    [NaughtyAttributes.Button] public void LoadMap() => LoadMap(temp);

    public void LoadMap(MapDataSO mapData)
    {
        if (clearCurrentScene)
        {
            Addressables.UnloadSceneAsync(currentLoadedScene).Completed += (asyncHandle) =>
            {
                clearCurrentScene = false;
                currentLoadedScene = new SceneInstance();
                Debug.Log("unloaded: " + CurrentMapData.name + " (" + CurrentMapData.mapID + ")");
            };
        }

        Addressables.LoadSceneAsync(mapData.mapID, LoadSceneMode.Additive).Completed += (asyncHandle) =>
        {
            clearCurrentScene = true;
            currentLoadedScene = asyncHandle.Result;
            CurrentMapData = mapData;

            var rootObjects = currentLoadedScene.Scene.GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                if (root.TryGetComponent<MapLocalManager>(out var localManager))
                {
                    localManager.Initialize();
                    this.localManager = localManager;
                    break;
                }
            }

            Debug.Log("loaded: " + mapData.name + " (" + mapData.mapID + ")");
        };
    }

    public void Oestroy()
    {
        Addressables.Release(currentLoadedScene);
    }
}