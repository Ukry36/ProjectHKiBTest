using UnityEditor;
using GraphProcessor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomEditor(typeof(StateMachineGraph), true)]
public class StateGraphAssetInspector : GraphInspector
{
	protected override void CreateInspector()
	{
		base.CreateInspector();

		SerializedProperty tsmProperty = serializedObject.FindProperty("targetStateMachine");

		if (tsmProperty != null)
		{
			ObjectField machineObjectField = new("Target State Machine")
			{
				objectType = typeof(StateMachineSO),
				allowSceneObjects = false
			};

			machineObjectField.BindProperty(tsmProperty);
			root.Insert(0, machineObjectField);

			machineObjectField.style.marginBottom = 12;
			machineObjectField.style.marginTop = 6;
		}

		root.Add(new Button(() => EditorWindow.GetWindow<StateMachineGraphWindow>().InitializeGraph(target as BaseGraph))
		{
			text = "Open graph window"
		});
	}
}
