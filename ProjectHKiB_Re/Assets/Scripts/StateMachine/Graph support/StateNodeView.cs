using UnityEditor;
using UnityEngine.UIElements;
using GraphProcessor;
using UnityEngine;

[NodeCustomEditor(typeof(StateNode))]
public class StateNodeView : BaseNodeView
{
    public override void Enable()
    {
        style.width = 400;

        var stateNode = nodeTarget as StateNode;

        if (stateNode.stateSO != null)
        {
            Button refreshButton = new(() => { stateNode.UpdateAllPorts(); })
            {
                text = "Refresh Transition Ports"
            };
            refreshButton.style.backgroundColor = new StyleColor(new Color(0.4f, 0.4f, 0.4f));
            controlsContainer.Add(refreshButton);

            var editor = Editor.CreateEditor(stateNode.stateSO);
            var inspectorIMGUI = new IMGUIContainer(() => {
                editor.OnInspectorGUI();
            });
            controlsContainer.Add(inspectorIMGUI);
        }
        else
        {
            controlsContainer.Add(new Label("StateSO is not current."));
        }
    }
}