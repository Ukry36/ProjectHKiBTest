
namespace UnityEditor.UI
{
    [CustomEditor(typeof(ButtonEnhanced), true)]
    [CanEditMultipleObjects]
    /// <summary>
    ///   Custom Editor for the Button Component.
    ///   Extend this class to write a custom editor for a component derived from Button.
    /// </summary>
    public class ButtonEnhancedCustomInspector : SelectableEditor
    {
        SerializedProperty m_OnClickProperty;
        SerializedProperty m_OnSelectProperty;
        SerializedProperty m_OnDeselectProperty;
        SerializedProperty m_TMPProperty;
        SerializedProperty m_OverrideNavigationProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_OnClickProperty = serializedObject.FindProperty("m_OnClick");
            m_OnSelectProperty = serializedObject.FindProperty("m_OnSelect");
            m_OnDeselectProperty = serializedObject.FindProperty("m_OnDeselect");
            m_TMPProperty = serializedObject.FindProperty("TMP");
            m_OverrideNavigationProperty = serializedObject.FindProperty("overrideNavigation");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_OverrideNavigationProperty);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_TMPProperty);
            EditorGUILayout.PropertyField(m_OnClickProperty);
            EditorGUILayout.PropertyField(m_OnSelectProperty);
            EditorGUILayout.PropertyField(m_OnDeselectProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
