#if UNITY_EDITOR
using System.Collections.Generic;
using GraphProcessor;
using UnityEditor;
using UnityEngine;

/// <summary>
/// StateMachineSO의 상태 목록과 저장된 그래프 표현을 증분 동기화한다.
/// 기존 배치와 연결은 보존하고 누락된 노드와 표준 전이 엣지만 추가한다.
/// </summary>
public static class StateMachineGraphSynchronizer
{
    private const float _nodeWidth = 400f;
    private const float _nodeHeight = 200f;
    private const float _columnGap = 160f;
    private const float _rowGap = 120f;
    private const int _maxRowsPerColumn = 6;

    /// <summary>
    /// StateMachine의 그래프를 확보하고 allStates 및 initialState를 그래프에 반영한다.
    /// 저장된 수동 배치를 지우지 않으며 실제 변경이 생긴 에셋만 Dirty 처리한다.
    /// </summary>
    public static StateMachineGraph Synchronize(StateMachineSO stateMachine)
    {
        if (stateMachine == null) return null;

        bool changed = false;
        StateMachineGraph graph = EnsureGraph(stateMachine, ref changed);
        if (graph == null) return null;

        Undo.RecordObject(graph, "Synchronize State Machine Graph");

        if (graph.targetStateMachine != stateMachine)
        {
            graph.targetStateMachine = stateMachine;
            changed = true;
        }

        InitialStateNode initialNode = null;
        var nodeOf = new Dictionary<StateSO, StateNode>();
        CollectExistingNodes(graph, nodeOf, ref initialNode);

        if (initialNode == null)
        {
            initialNode = BaseNode.CreateFromType<InitialStateNode>(Vector2.zero);
            graph.AddExistingNode(initialNode);
            changed = true;
        }

        List<StateSO> states = CollectStates(stateMachine);
        Vector2 firstNewPosition = FindFirstNewNodePosition(graph);
        int addedStateCount = 0;

        for (int i = 0; i < states.Count; i++)
        {
            StateSO state = states[i];
            if (nodeOf.ContainsKey(state)) continue;

            StateNode node = CreateStateNode(
                graph,
                state,
                GetNewNodePosition(firstNewPosition, addedStateCount));
            nodeOf.Add(state, node);
            addedStateCount++;
            changed = true;
        }

        if (addedStateCount > 0 && graph.nodes.Count == addedStateCount + 1)
            PositionInitialNode(initialNode, stateMachine.initialState, nodeOf);

        changed |= SynchronizeInitialEdge(graph, initialNode, stateMachine, nodeOf);
        changed |= AddMissingTransitionEdges(graph, nodeOf);

        if (changed)
        {
            EditorUtility.SetDirty(graph);
            EditorUtility.SetDirty(stateMachine);
            AssetDatabase.SaveAssetIfDirty(graph);
            AssetDatabase.SaveAssetIfDirty(stateMachine);
        }

        return graph;
    }

    /// <summary>
    /// StateMachine에 Graph 서브 에셋이 없으면 새로 만들고 참조를 연결한다.
    /// 저장되지 않은 임시 StateMachine에는 서브 에셋을 만들 수 없으므로 오류 후 중단한다.
    /// </summary>
    private static StateMachineGraph EnsureGraph(StateMachineSO stateMachine, ref bool changed)
    {
        if (stateMachine.graph != null) return stateMachine.graph;

        string assetPath = AssetDatabase.GetAssetPath(stateMachine);
        if (string.IsNullOrWhiteSpace(assetPath))
        {
            Debug.LogError(
                $"[StateMachineGraphSynchronizer] '{stateMachine.name}'이 저장된 에셋이 아니어서 그래프를 만들 수 없습니다.",
                stateMachine);
            return null;
        }

        Undo.RecordObject(stateMachine, "Create State Machine Graph");
        StateMachineGraph graph = ScriptableObject.CreateInstance<StateMachineGraph>();
        graph.name = stateMachine.name + "Editor";
        graph.targetStateMachine = stateMachine;
        AssetDatabase.AddObjectToAsset(graph, stateMachine);
        Undo.RegisterCreatedObjectUndo(graph, "Create State Machine Graph");
        stateMachine.graph = graph;
        changed = true;
        return graph;
    }

    /// <summary>
    /// 그래프의 기존 Initial/State 노드를 수집하여 중복 생성을 막는다.
    /// 같은 StateSO를 가리키는 노드가 여럿이면 기존 그래프를 건드리지 않고 첫 노드를 사용한다.
    /// </summary>
    private static void CollectExistingNodes(
        StateMachineGraph graph,
        Dictionary<StateSO, StateNode> nodeOf,
        ref InitialStateNode initialNode)
    {
        for (int i = 0; i < graph.nodes.Count; i++)
        {
            BaseNode node = graph.nodes[i];
            if (node is InitialStateNode foundInitial && initialNode == null)
            {
                initialNode = foundInitial;
                continue;
            }

            if (node is StateNode stateNode &&
                stateNode.stateSO != null &&
                !nodeOf.ContainsKey(stateNode.stateSO))
                nodeOf.Add(stateNode.stateSO, stateNode);
        }
    }

    /// <summary>
    /// Initial State를 먼저, allStates를 그다음 순서로 중복 없이 수집한다.
    /// initialState가 목록에서 빠진 오래된 에셋도 그래프에는 정상 표시되게 한다.
    /// </summary>
    private static List<StateSO> CollectStates(StateMachineSO stateMachine)
    {
        var states = new List<StateSO>();
        var seen = new HashSet<StateSO>();

        if (stateMachine.initialState != null && seen.Add(stateMachine.initialState))
            states.Add(stateMachine.initialState);

        if (stateMachine.allStates == null) return states;
        for (int i = 0; i < stateMachine.allStates.Count; i++)
        {
            StateSO state = stateMachine.allStates[i];
            if (state != null && seen.Add(state)) states.Add(state);
        }

        return states;
    }

    /// <summary>
    /// 기존 그래프 오른쪽의 빈 열을 새 노드 배치 시작점으로 계산한다.
    /// 상태 노드가 없다면 원점을 사용하여 처음 생성되는 그래프를 단순하게 유지한다.
    /// </summary>
    private static Vector2 FindFirstNewNodePosition(StateMachineGraph graph)
    {
        bool foundStateNode = false;
        float right = 0f;

        for (int i = 0; i < graph.nodes.Count; i++)
        {
            if (graph.nodes[i] is not StateNode stateNode) continue;
            right = foundStateNode ? Mathf.Max(right, stateNode.position.xMax) : stateNode.position.xMax;
            foundStateNode = true;
        }

        return foundStateNode
            ? new Vector2(right + _columnGap, 0f)
            : Vector2.zero;
    }

    /// <summary>
    /// 새 노드를 한 열에 제한된 개수만큼 배치한 뒤 오른쪽 열로 넘긴다.
    /// 자동 생성 패턴이 많아져도 한 방향으로 끝없이 길어지지 않게 한다.
    /// </summary>
    private static Vector2 GetNewNodePosition(Vector2 origin, int index)
    {
        float cellWidth = _nodeWidth + _columnGap;
        float cellHeight = _nodeHeight + _rowGap;
        return origin + new Vector2(
            index / _maxRowsPerColumn * cellWidth,
            index % _maxRowsPerColumn * cellHeight);
    }

    /// <summary>
    /// 기존 StateSO를 참조하는 StateNode를 만들고 BaseGraph 경로로 등록한다.
    /// StateMachineGraph.AddNode의 새 StateSO 생성 동작은 의도적으로 거치지 않는다.
    /// </summary>
    private static StateNode CreateStateNode(
        StateMachineGraph graph,
        StateSO state,
        Vector2 position)
    {
        StateNode node = BaseNode.CreateFromType<StateNode>(position);
        node.position.size = new Vector2(_nodeWidth, _nodeHeight);
        node.stateSO = state;
        graph.AddExistingNode(node);
        node.SetCustomName(state.name);
        return node;
    }

    /// <summary>
    /// 새 그래프의 Initial 노드를 실제 초기 상태 노드 왼쪽에 배치한다.
    /// 기존 그래프에서는 사용자가 정리한 Initial 노드 위치를 변경하지 않는다.
    /// </summary>
    private static void PositionInitialNode(
        InitialStateNode initialNode,
        StateSO initialState,
        Dictionary<StateSO, StateNode> nodeOf)
    {
        if (initialState == null || !nodeOf.TryGetValue(initialState, out StateNode stateNode))
            return;

        initialNode.position.position = new Vector2(
            stateNode.position.x - _nodeWidth - _columnGap,
            stateNode.position.y);
    }

    /// <summary>
    /// Initial 출력을 StateMachineSO.initialState와 일치하도록 생성 또는 갱신한다.
    /// 데이터에서 초기 State가 바뀌면 오래된 그래프 엣지만 제거하고 노드 배치는 보존한다.
    /// </summary>
    private static bool SynchronizeInitialEdge(
        StateMachineGraph graph,
        InitialStateNode initialNode,
        StateMachineSO stateMachine,
        Dictionary<StateSO, StateNode> nodeOf)
    {
        StateNode targetNode = null;
        if (stateMachine.initialState != null)
            nodeOf.TryGetValue(stateMachine.initialState, out targetNode);

        return SynchronizeOutputEdge(
            graph,
            initialNode,
            nameof(InitialStateNode.outputTransitions),
            null,
            targetNode);
    }

    /// <summary>
    /// 각 StateSO의 기본 transitions 배열을 따라 아직 없는 그래프 엣지를 보충한다.
    /// identifier가 겹치는 additionalTransitions는 기존 그래프 규칙에 맞춰 자동 연결하지 않는다.
    /// </summary>
    private static bool AddMissingTransitionEdges(
        StateMachineGraph graph,
        Dictionary<StateSO, StateNode> nodeOf)
    {
        bool changed = false;

        foreach (KeyValuePair<StateSO, StateNode> pair in nodeOf)
        {
            StateTransition[] transitions = pair.Key.transitions;
            if (transitions == null) continue;
            changed |= pair.Value.UpdateAllPortsLocal();

            for (int i = 0; i < transitions.Length; i++)
            {
                StateTransition transition = transitions[i];
                if (transition == null) continue;

                changed |= AddTransitionEdgeIfMissing(
                    graph,
                    pair.Value,
                    transition.trueState,
                    transition.showTrueStatePort,
                    $"T_{i}_True",
                    nodeOf);
                changed |= AddTransitionEdgeIfMissing(
                    graph,
                    pair.Value,
                    transition.falseState,
                    transition.showFalseStatePort,
                    $"T_{i}_False",
                    nodeOf);
            }
        }

        return changed;
    }

    /// <summary>
    /// 한 전이 출력 포트를 StateSO에 저장된 목적지와 일치하도록 생성·제거·갱신한다.
    /// 자동 체인 재배선 뒤 그래프가 예전 목적지를 계속 보여 주지 않게 한다.
    /// </summary>
    private static bool AddTransitionEdgeIfMissing(
        StateMachineGraph graph,
        StateNode sourceNode,
        StateSO targetState,
        bool showPort,
        string identifier,
        Dictionary<StateSO, StateNode> nodeOf)
    {
        StateNode targetNode = null;
        if (showPort && targetState != null)
            nodeOf.TryGetValue(targetState, out targetNode);

        return SynchronizeOutputEdge(
            graph,
            sourceNode,
            nameof(StateNode.outputTransitions),
            identifier,
            targetNode);
    }

    /// <summary>
    /// 특정 출력 포트의 엣지를 요청 목적지 하나와 일치하도록 증분 동기화한다.
    /// 올바른 엣지는 유지하고 오래되거나 중복된 레코드만 포트와 그래프에서 제거한다.
    /// </summary>
    private static bool SynchronizeOutputEdge(
        StateMachineGraph graph,
        BaseNode sourceNode,
        string outputFieldName,
        string outputIdentifier,
        StateNode targetNode)
    {
        bool changed = false;
        bool foundExpectedEdge = false;

        for (int i = graph.edges.Count - 1; i >= 0; i--)
        {
            SerializableEdge edge = graph.edges[i];
            if (edge == null) continue;
            if (edge.outputNode == null) edge.Deserialize();

            if (edge.outputNode != sourceNode ||
                edge.outputFieldName != outputFieldName ||
                edge.outputPortIdentifier != outputIdentifier)
                continue;

            if (!foundExpectedEdge && targetNode != null && edge.inputNode == targetNode)
            {
                foundExpectedEdge = true;
                continue;
            }

            RemoveEdgeRecord(graph, edge);
            changed = true;
        }

        if (targetNode != null && !foundExpectedEdge)
            changed |= AddEdge(
                graph,
                sourceNode,
                outputFieldName,
                outputIdentifier,
                targetNode);

        return changed;
    }

    /// <summary>
    /// StateSO 참조를 건드리는 OnEdgeDisconnected를 호출하지 않고 직렬화 엣지만 제거한다.
    /// 활성 그래프의 포트 캐시에서도 같은 레코드를 빼 재연결 시 낡은 점유 정보가 남지 않게 한다.
    /// </summary>
    private static void RemoveEdgeRecord(StateMachineGraph graph, SerializableEdge edge)
    {
        edge.inputNode?.inputPorts.Remove(edge);
        edge.outputNode?.outputPorts.Remove(edge);
        graph.edges.Remove(edge);
        if (!string.IsNullOrEmpty(edge.GUID)) graph.edgesPerGUID.Remove(edge.GUID);
    }

    /// <summary>
    /// State 데이터는 이미 배선되어 있으므로 부작용 없는 직렬화 엣지를 직접 추가한다.
    /// 포트를 찾지 못하면 그래프 손상을 피하고 경고만 남긴다.
    /// </summary>
    private static bool AddEdge(
        StateMachineGraph graph,
        BaseNode sourceNode,
        string outputFieldName,
        string outputIdentifier,
        StateNode targetNode)
    {
        NodePort outputPort = sourceNode.GetPort(outputFieldName, outputIdentifier);
        NodePort inputPort = targetNode.GetPort(nameof(StateNode.inputState), null);
        if (outputPort == null || inputPort == null)
        {
            Debug.LogWarning(
                $"[StateMachineGraphSynchronizer] 포트를 찾지 못해 연결을 건너뜁니다: " +
                $"{sourceNode.name}.{outputFieldName}[{outputIdentifier}] -> {targetNode.name}",
                graph);
            return false;
        }

        SerializableEdge edge = SerializableEdge.CreateNewEdge(graph, inputPort, outputPort);
        graph.edges.Add(edge);
        graph.edgesPerGUID[edge.GUID] = edge;
        targetNode.inputPorts.Add(edge);
        sourceNode.outputPorts.Add(edge);
        return true;
    }
}
#endif
