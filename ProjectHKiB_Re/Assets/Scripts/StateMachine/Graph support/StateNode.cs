using GraphProcessor;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[System.Serializable, NodeMenuItem("State Machine/State Node")]
public class StateNode : BaseNode
{
    [NaughtyAttributes.Label("")] public StateSO stateSO;

    [Input(name = "In")]
    public StateSO inputState;

    [Output(name = "Transitions")]
    public StateSO outputTransitions;

    public override string name => stateSO != null ? stateSO.name : "Empty State";
    public override bool isRenamable => true;
    public override bool needsInspector => true;

    public override void SetCustomName(string customName)
    {
        base.SetCustomName(customName);

        if (stateSO != null && stateSO.name != customName)
        {
            Undo.RecordObject(stateSO, "Rename State Asset");

            stateSO.name = customName;

            EditorUtility.SetDirty(stateSO);

            if (graph != null)
            {
                EditorUtility.SetDirty(graph);
                AssetDatabase.SaveAssetIfDirty(stateSO);
            }
        }
    }

    [CustomPortBehavior(nameof(outputTransitions))]
    IEnumerable<PortData> GetPortsForTransitions(List<SerializableEdge> edges)
    {
        if (stateSO == null || stateSO.transitions == null) yield break;

        for (int i = 0; i < stateSO.transitions.Length; i++)
        {
            if (stateSO.transitions[i].showTrueStatePort) // True Port
                yield return new PortData
                {
                    displayName = $"{stateSO.transitions[i].name} (True)",
                    displayType = typeof(StateSO),
                    identifier = $"T_{i}_True"
                };

            if (stateSO.transitions[i].showFalseStatePort) // False Port
                yield return new PortData
                {
                    displayName = $"{stateSO.transitions[i].name} (False)",
                    displayType = typeof(StateSO),
                    identifier = $"T_{i}_False"
                };
        }
        for (int i = 0; i < stateSO.additionalTransitions.Length; i++)
        {
            if (stateSO.additionalTransitions[i].showTrueStatePort) // True Port
                yield return new PortData
                {
                    displayName = $"{stateSO.additionalTransitions[i].name} (True)",
                    displayType = typeof(StateSO),
                    identifier = $"T_{i}_True"
                };

            if (stateSO.additionalTransitions[i].showFalseStatePort) // False Port
                yield return new PortData
                {
                    displayName = $"{stateSO.additionalTransitions[i].name} (False)",
                    displayType = typeof(StateSO),
                    identifier = $"T_{i}_False"
                };
        }
    }

    protected override void Process() { }

    public override void OnEdgeConnected(SerializableEdge edge)
    {
        if (edge.outputNode == this && stateSO != null)
        {
            var targetNode = edge.inputNode as StateNode;
            if (targetNode == null || targetNode.stateSO == null) return;

            ParseIdentifierAndAssign(edge.outputPort.portData.identifier, targetNode.stateSO);
        }
        base.OnEdgeConnected(edge);
    }

    public override void OnEdgeDisconnected(SerializableEdge edge)
    {
        if (edge.outputNode == this && stateSO != null)
        {
            ParseIdentifierAndAssign(edge.outputPort.portData.identifier, null);
        }
        base.OnEdgeDisconnected(edge);
    }

    private void ParseIdentifierAndAssign(string identifier, StateSO targetStateSO)
    {
        if (identifier.StartsWith("T_"))
        {
            string[] parts = identifier.Split('_');

            // parts[0] = "T", parts[1] = index, parts[2] = "True" or "False"
            if (parts.Length == 3 && int.TryParse(parts[1], out int index))
            {
                bool isTrueState = parts[2] == "True";

                if (index >= 0 && index < stateSO.transitions.Length)
                {
                    if (isTrueState)
                        stateSO.transitions[index].trueState = targetStateSO;
                    else
                        stateSO.transitions[index].falseState = targetStateSO;

                    EditorUtility.SetDirty(stateSO);

                }
            }
        }
    }
}