using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapChangeManager : MonoBehaviour
{
    [NaughtyAttributes.Scene] public string currentMap;
    [NaughtyAttributes.Scene] public string togo;

    public bool trig;
    private float time;

    public void ChangeMap(string mapName)
    {
        GameManager.instance.chunkManager.UnregisterChunkDataAll();
        if (currentMap != null)
        {
            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentMap);
            unloadOp.completed += (op) => StartCoroutine(LoadMap(mapName));
        }
        else
            StartCoroutine(LoadMap(mapName));
    }

    private IEnumerator LoadMap(string mapName)
    {
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(mapName, LoadSceneMode.Additive);
        time = Time.time;
        while (!loadOp.isDone)
        {
            LoadingProgress(loadOp.progress);
            yield return null;
        }
        yield return null;
        AfterLoadMap(mapName);
    }

    private void AfterLoadMap(string mapName)
    {
        Debug.Log(mapName);
        MapStartPos[] startPoses = FindObjectsOfType<MapStartPos>();
        for (int i = 0; i < startPoses.Length; i++)
        {
            if (startPoses[i].fromScene.Equals(currentMap))
            {
                startPoses[i].SetPlayerToStartPos();
            }

        }
        currentMap = mapName;
        //GameManager.instance.chunkManager.RegisterChunkDataAll();
    }

    private void LoadingProgress(float progress)
    {
        Debug.Log(progress);
    }

    // Update is called once per frame
    void Update()
    {
        if (trig) ChangeMap(togo);
        trig = false;
    }
}
