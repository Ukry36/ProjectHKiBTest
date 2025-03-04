using System;
using UnityEngine;

[Serializable]
public class Line
{
    public string characterName;
    public string line;
    public float duration;
    public Sprite BG;
    public StandingCGControlData[] standingCGControlDatas;

    public Line()
    {
        if (duration <= 0)
        {
            duration = line.Length * 0.1f;
        }
    }
}
