using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StateSO))]
public class StateSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        StateSO stateSO = (StateSO)target;
        serializedObject.Update();

        if (stateSO.isPacked) DrawPackedMode(stateSO);
        else DrawSetupMode(stateSO);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPackedMode(StateSO stateSO)
    {
        foreach (var exposed in stateSO.exposedVariables)
        {
            SerializedProperty prop = serializedObject.FindProperty(exposed.propertyPath);
            if (prop != null) EditorGUILayout.PropertyField(prop, new GUIContent(exposed.displayName), true);
            else EditorGUILayout.LabelField($"Can't find the path: {exposed.displayName}");
        }

        GUILayout.Space(15);
        if (GUILayout.Button("Unpack"))
        {
            stateSO.isPacked = false;
            EditorUtility.SetDirty(stateSO);
        }
        if (GUILayout.Button("Make Template"))
        {
            MakeTemplate(stateSO);
        }
        if (GUILayout.Button("Load Template"))
        {
            ShowTemplateSelectorMenu(stateSO);
        }
        if (stateSO.isTemplate) EditorGUILayout.HelpBox("This is template state", MessageType.Info);
    }

    private void DrawSetupMode(StateSO stateSO)
    {
        // draw default inspector
        DrawPropertiesExcluding(serializedObject, "m_Script", "isPacked", "isTemplate", "exposedVariables");

        GUILayout.Space(20);
        EditorGUILayout.LabelField("Packing Setting", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(serializedObject.FindProperty("exposedVariables"), new GUIContent("Variables to Pack"), true);

        GUILayout.Space(5);
        if (GUILayout.Button("Add Variable", GUILayout.Height(25)))
        {
            ShowPropertySelectorMenu(stateSO);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Pack"))
        {
            stateSO.isPacked = true;
            EditorUtility.SetDirty(stateSO);
        }
        if (GUILayout.Button("Make Template"))
        {
            MakeTemplate(stateSO);
        }
        if (GUILayout.Button("Load Template"))
        {
            ShowTemplateSelectorMenu(stateSO);
        }
        if (stateSO.isTemplate) EditorGUILayout.HelpBox("This is template state", MessageType.Info);
    }

    private void MakeTemplate(StateSO stateSO)
    {
        string targetPath = $"Assets/ScriptableObjects/StateTemplates/{stateSO.name}.asset";
        bool proceedCreation = true;

        StateSO existingTemplate = AssetDatabase.LoadAssetAtPath<StateSO>(targetPath);

        if (existingTemplate != null)
        {
            proceedCreation = EditorUtility.DisplayDialog(
                "Template Overwrite Warning",
                $"A template named '{stateSO.name}' already exists.\nDo you really want to overwrite it?",
                "Overwrite",
                "Cancel"
            );
        }

        if (proceedCreation)
        {
            StateSO instance = Instantiate(stateSO);
            instance.isPacked = true;
            instance.isTemplate = true;

            AssetDatabase.CreateAsset(instance, targetPath);

            EditorUtility.SetDirty(instance);
            AssetDatabase.SaveAssets();

            Debug.Log($"Template of {stateSO.name} is generated.");
        }
    }

    private void ShowTemplateSelectorMenu(StateSO stateSO)
    {
        GenericMenu menu = new();
        string[] searchFolders = new string[] { "Assets/ScriptableObjects/StateTemplates" };
        string[] guids = AssetDatabase.FindAssets("t:StateSO", searchFolders);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StateSO template = AssetDatabase.LoadAssetAtPath<StateSO>(path);
            string menuPath = $"{template.name}";
            menu.AddItem(new GUIContent(menuPath), false, () =>
            {
                Undo.RecordObject(stateSO, "Load Template");
                string name = template.name;
                EditorUtility.CopySerialized(template, stateSO);
                stateSO.name = name;
                stateSO.isTemplate = false;
                EditorUtility.SetDirty(stateSO);
            });
        }

        menu.ShowAsContext();
    }

    private void ShowPropertySelectorMenu(StateSO stateSO)
    {
        GenericMenu menu = new();
        SerializedProperty iterator = serializedObject.GetIterator();

        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = true;

            // If you use the iterator directly inside GenericMenu's callback,
            // only the last value is referenced. So it must be copied as a local variable.
            string propPath = iterator.propertyPath;
            string propName = iterator.displayName;

            if (propPath == "m_Script" ||
                propPath == "isPacked" ||
                propPath == "isTemplate" ||
                propPath.StartsWith("exposedVariables") ||
                propPath == "EnterActions.Array.size" ||
                propPath.EndsWith(".Array.size") ||
                propPath.EndsWith("]"))
            {
                continue;
            }

            // menu nesting!
            string menuPath = propPath.Replace(".Array.data[", "[").Replace('.', '/');

            menu.AddItem(new GUIContent(menuPath), false, () =>
            {
                stateSO.exposedVariables.Add(new ExposedVariable
                {
                    propertyPath = propPath,
                    displayName = propName
                });

                EditorUtility.SetDirty(stateSO);
                serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }
}