using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PlayerSkillDataSO))]
public class GraffitiSkillCustomInspector : Editor
{
    Texture2D blackSquare;
    Texture2D redSquare;
    PlayerSkillDataSO skill;
    int remove;
    bool calculate;
    void OnEnable()
    {
        redSquare = Resources.Load<Texture2D>("customInspector/redSquare");
        blackSquare = Resources.Load<Texture2D>("customInspector/blackSquare");
        skill = target as PlayerSkillDataSO;
        remove = -1;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.LabelField("graffitiCodes");
        if (GUILayout.Button("+"))
        {
            calculate = true;
            skill.graffitiCodes ??= new();

            skill.graffitiCodes.Add(new());
            skill.graffitiCodes[^1].code.Add(Vector2Int.zero);
        }

        if (skill.graffitiCodes != null)
        {

            for (int i = 0; i < skill.graffitiCodes.Count; i++)
            {
                GUILayout.Space(10);
                float maxX = 0;
                for (int j = 0; j < skill.graffitiCodes[i].code.Count; j++)
                    if (maxX < skill.graffitiCodes[i].code[j].x)
                        maxX = skill.graffitiCodes[i].code[j].x;
                float maxY = 0;
                for (int j = 0; j < skill.graffitiCodes[i].code.Count; j++)
                    if (maxY < skill.graffitiCodes[i].code[j].y)
                        maxY = skill.graffitiCodes[i].code[j].y;

                EditorGUILayout.BeginVertical();
                for (int y = (int)maxY + 1; y >= 0; y--)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int x = 0; x <= maxX + 1; x++)
                    {
                        bool temp = true;
                        for (int j = 0; j < skill.graffitiCodes[i].code.Count; j++)
                        {
                            if (skill.graffitiCodes[i].code[j].x == x && skill.graffitiCodes[i].code[j].y == y)
                            {
                                if (GUILayout.Button(redSquare,
                                GUILayout.Width(30), GUILayout.Height(30)))
                                {
                                    calculate = true;
                                    skill.graffitiCodes[i].code.Remove(new Vector2Int(x, y));
                                }
                                temp = false;
                            }
                        }
                        if (temp)
                        {
                            if (GUILayout.Button(blackSquare,
                            GUILayout.Width(30), GUILayout.Height(30)))
                            {
                                calculate = true;
                                skill.graffitiCodes[i].code.Add(new Vector2Int(x, y));
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("-")) remove = i;
                EditorGUILayout.EndVertical();
            }
            if (remove >= 0 && skill.graffitiCodes.Count >= remove)
            {
                skill.graffitiCodes.Remove(skill.graffitiCodes[remove]);
                remove = -1;
                calculate = true;
            }
            if (calculate)
                skill.CalculateAllCases();
        }
    }
}