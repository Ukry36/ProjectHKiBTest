using System;
using UnityEngine;

[Serializable]
public class Line
{
    public string name;
    public string characterName;

    [TextArea]
    public string[] sublines;
    public Sprite BG;
    public bool isChoice;
    [NaughtyAttributes.ShowIf("isChoice")] public string[] choices;

    public float Duration(int sublineNum)
    {
        if (sublines != null && sublines.Length > 0)
        {
            return sublines[sublineNum].Length * 0.1f;
        }
        return 10f;
    }
}
