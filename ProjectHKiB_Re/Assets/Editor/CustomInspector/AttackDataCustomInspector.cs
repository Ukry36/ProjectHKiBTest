using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(AttackDataSO))]
public class AttackDataCustomInspector : Editor
{
    BoxData data;
    readonly float size = 2f;
    void OnEnable()
    {
        data = (target as AttackDataSO).attackAreaIndicatorData.downwardIndicatorArea;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.LabelField("Preview");
        bool isdamageMaxBigger = 0.5f < data.offset.y + data.size.y * 0.5f;

        float maxYDiff = MathF.Abs(data.offset.y + data.size.y * 0.5f - 0.5f);

        EditorGUILayout.BeginScrollView
        (
            Vector2.zero,
            GUILayout.Height((maxYDiff + data.size.y) * 10 * size)
        );
        EditorGUILayout.Space();
        Rect rect = GUILayoutUtility.GetLastRect();
        EditorGUI.DrawRect(
            new Rect
            (
                rect.center.x + (data.offset.x * 10 - data.size.x * 5) * size,
                isdamageMaxBigger ? rect.y : rect.y + maxYDiff * size * 10,
                data.size.x * 10 * size,
                data.size.y * 10 * size
            ),
        Color.red);

        EditorGUI.DrawRect(
            new Rect
            (
                rect.center.x - 5 * size,
                isdamageMaxBigger ? rect.y + maxYDiff * size * 10 : rect.y,
                10 * size,
                10 * size
            ),
        Color.black);

        EditorGUI.DrawRect(
            new Rect
            (
                rect.center.x - 2 * size + data.pivot.x * 10 * size,
                -data.pivot.y * 10 * size + (isdamageMaxBigger ? rect.y + maxYDiff * size * 10 : rect.y) + 3 * size,
                size * 4,
                size * 4
            ),
        Color.yellow);
        EditorGUILayout.EndScrollView();
    }
}