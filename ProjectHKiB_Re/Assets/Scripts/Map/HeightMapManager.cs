using System.Collections.Generic;
using UnityEngine;

public class HeightMapManager: MonoBehaviour
{
    public RenderTexture test;
    public List<RelativeHeightMap> relativeHeightMaps;

    [NaughtyAttributes.Button]
    public void Update()
    {
        Shader.SetGlobalTexture("_RelativeHeight", test);
        for (int i = 0; i < relativeHeightMaps.Count; i++)
        {
            relativeHeightMaps[i].SetRelativeZlevel();
        }
    }
}