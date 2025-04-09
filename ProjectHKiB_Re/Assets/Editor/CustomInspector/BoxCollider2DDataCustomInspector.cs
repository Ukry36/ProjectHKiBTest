using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

[CustomEditor(typeof(DamageDataSO))]
public class DamageDataCustomInspector : Editor
{
    DamageDataSO data;
    readonly float size = 2f;
    void OnEnable()
    {
        data = target as DamageDataSO;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.LabelField("Preview");
        float damageAreaOffsety;
        float damagerOffsety;
        if (Mathf.Abs(data.downwardDamageArea.offset.y) > data.downwardDamageArea.size.y / 2)
        {
            EditorGUILayout.Space((Mathf.Abs(data.downwardDamageArea.offset.y) * 10 + data.downwardDamageArea.size.y * 5 + 20) * size);

            if (data.downwardDamageArea.offset.y < 0)
            {
                damageAreaOffsety = -data.downwardDamageArea.offset.y * 5 - data.downwardDamageArea.size.y * 2.5f - 20;
                damagerOffsety = data.downwardDamageArea.offset.y * 5 + data.downwardDamageArea.size.y * 2.5f - 25;
            }
            else
            {
                damageAreaOffsety = -data.downwardDamageArea.offset.y * 5 - data.downwardDamageArea.size.y * 7.5f + 20;
                damagerOffsety = data.downwardDamageArea.offset.y * 5 - data.downwardDamageArea.size.y * 2.5f + 15;
            }
        }
        else
        {
            EditorGUILayout.Space((data.downwardDamageArea.size.y * 10 + 20) * size);
            damageAreaOffsety = -data.downwardDamageArea.size.y * 5;
            damagerOffsety = data.downwardDamageArea.offset.y * 10 - 5;
        }


        Rect rect = GUILayoutUtility.GetLastRect();
        EditorGUI.DrawRect(
            new Rect
            (
                rect.center.x + (data.downwardDamageArea.offset.x * 10 - data.downwardDamageArea.size.x * 5) * size,
                rect.center.y + damageAreaOffsety * size,
                data.downwardDamageArea.size.x * 10 * size,
                data.downwardDamageArea.size.y * 10 * size
            ),
        Color.red);

        EditorGUI.DrawRect(
            new Rect
            (
                rect.center.x - 5 * size,
                rect.center.y + damagerOffsety * size,
                10 * size,
                10 * size
            ),
        Color.cyan);
    }

}

