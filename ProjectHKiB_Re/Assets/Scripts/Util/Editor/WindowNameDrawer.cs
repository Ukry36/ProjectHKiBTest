#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// [WindowName] string 필드의 드로어. 이름을 손으로 치다 한 글자 어긋나면 UIManager가 경고만 남기고 조용히
// 넘어가므로(OpenWindowAction 주석 참고), 등록된 이름을 고르거나 하이라키의 Window 오브젝트를 끌어다 놓게 한다.
//
// 이름 목록은 열린 씬의 UIManager.windows에서 읽는다(GameManager/UIManager는 씬 오브젝트). 씬에 UIManager가 없으면
// 드롭다운 대신 텍스트 필드만 보인다. 목록에 없는 값은 "⚠ 없는 창: …"으로 보존해 조용히 바뀌지 않게 한다.
[CustomPropertyDrawer(typeof(WindowNameAttribute))]
public class WindowNameDrawer : PropertyDrawer
{
    private const float ObjectFieldWidth = 120f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
        EditorGUIUtility.singleLineHeight;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        EditorGUI.BeginProperty(position, label, property);
        UIManager ui = Object.FindObjectOfType<UIManager>(true);
        List<UIManager.WindowItem> items = ui != null && ui.windows != null ? ui.windows : null;

        Rect main = new Rect(position.x, position.y, position.width - ObjectFieldWidth - 4f, position.height);
        Rect drop = new Rect(main.xMax + 4f, position.y, ObjectFieldWidth, position.height);

        string current = property.stringValue ?? "";
        if (items == null || items.Count == 0)
        {
            // UIManager가 없는 씬(프리팹 편집 등)에서는 목록을 만들 수 없다. 이름은 그대로 손으로 친다.
            EditorGUI.PropertyField(main, property, label);
        }
        else
        {
            var names = new List<string>();
            var labels = new List<string>();
            foreach (UIManager.WindowItem item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.name) || names.Contains(item.name)) continue;
                names.Add(item.name);
                labels.Add(item.window != null ? $"{item.name}  ({item.window.gameObject.name})" : item.name);
            }
            int index = names.IndexOf(current);
            if (index < 0)
            {
                index = names.Count;
                names.Add(current);
                labels.Add(string.IsNullOrEmpty(current) ? "(비어 있음)" : $"⚠ 없는 창: {current}");
            }
            int next = EditorGUI.Popup(main, label.text, index, labels.ToArray());
            if (next != index) property.stringValue = names[next];
        }

        // 하이라키에서 Window(또는 그 GameObject)를 끌어다 놓으면 등록명을 찾아 넣는다. 등록돼 있지 않은 오브젝트면
        // 오브젝트 이름을 넣되 경고를 남긴다 — 그 이름으로 UIManager가 찾지 못할 수 있어서다.
        EditorGUI.BeginChangeCheck();
        Object dropped = EditorGUI.ObjectField(drop, null, typeof(Object), true);
        if (EditorGUI.EndChangeCheck() && dropped != null)
        {
            Window window = dropped as Window ?? (dropped as GameObject)?.GetComponent<Window>();
            string resolved = null;
            if (items != null && window != null)
                foreach (UIManager.WindowItem item in items)
                    if (item != null && item.window == window) { resolved = item.name; break; }
            if (resolved == null)
            {
                resolved = window != null ? window.gameObject.name : dropped.name;
                Debug.LogWarning($"[WindowNameDrawer] '{dropped.name}'은(는) UIManager.windows에 등록돼 있지 않습니다. 오브젝트 이름 '{resolved}'을(를) 넣었으니 등록명과 맞는지 확인하세요.");
            }
            property.stringValue = resolved;
        }
        EditorGUI.EndProperty();
    }
}
#endif
