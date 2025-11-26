using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue Data", menuName = "Scriptable Objects/Data/Dialogue Data", order = 0)]

public class DialogueDataSO : ScriptableObject
{
    public List<Line> lines;

    public string summaryTitle;
    public string summary;
}