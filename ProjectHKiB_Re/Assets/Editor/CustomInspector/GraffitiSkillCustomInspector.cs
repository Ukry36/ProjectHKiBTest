using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlayerSkillDataSO))]
public class GraffitiSkillCustomInspector : Editor
{

    PlayerSkillDataSO skill;
    void OnEnable()
    {
        skill = target as PlayerSkillDataSO;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.LabelField("graffitiCodes");
        for (int o = 0; o < skill.graffitiCodes.Count; o++)
        {
            float maxX = 0;
            for (int j = 0; j < skill.graffitiCodes.Count; j++)
                if (maxX < skill.graffitiCodes[o][j].x)
                    maxX = skill.graffitiCodes[o][j].x;
            float maxY = 0;
            for (int j = 0; j < skill.graffitiCodes.Count; j++)
                if (maxY < skill.graffitiCodes[o][j].y)
                    maxY = skill.graffitiCodes[o][j].y;

            EditorGUILayout.BeginVertical();
            for (int y = (int)maxY + 1; y >= 0; y--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = 0; x <= maxX + 1; x++)
                {
                    bool temp = true;
                    for (int j = 0; j < skill.graffitiCodes.Count; j++)
                    {
                        if (skill.graffitiCodes[o][j].x == x && skill.graffitiCodes[o][j].y == y)
                        {
                            if (GUILayout.Button(Resources.Load<Texture2D>("ui/whiteSquare1"),
                            GUILayout.Width(30), GUILayout.Height(30)))
                            {
                                skill.graffitiCodes[o].Remove(new Vector2(x, y));
                            }
                            temp = false;
                        }
                    }
                    if (temp)
                    {
                        if (GUILayout.Button(Resources.Load<Texture2D>("ui/whiteSquare0"),
                        GUILayout.Width(30), GUILayout.Height(30)))
                        {
                            skill.graffitiCodes[o].Add(new Vector2(x, y));
                        }
                    }

                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

    }
}