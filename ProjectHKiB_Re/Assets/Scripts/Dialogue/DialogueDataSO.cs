using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue Data", menuName = "Scriptable Objects/Data/Dialogue Data", order = 0)]

public class DialogueDataSO : ScriptableObject
{
    public Line[] lines;
    //Find Next Index and Fitst Index Line
    public int FindLineIndexByLogicalIndex(int logicalIndex)
    {
        if (lines == null || lines.Length == 0)
            return -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].index == logicalIndex)
                return i;
        }

        return -1;
    }

    public string summaryTitle;
    public string summary;
}