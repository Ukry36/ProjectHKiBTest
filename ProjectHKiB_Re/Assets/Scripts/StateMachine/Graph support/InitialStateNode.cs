using GraphProcessor;

[System.Serializable, NodeMenuItem("State Machine/Initial State Node")]
public class InitialStateNode : BaseNode
{

    [Output(name = "Initial")]
    public StateSO outputTransitions;

    public override string name => "Initial State";

    protected override void Process() { }

    public override void OnEdgeConnected(SerializableEdge edge)
    {
        if (edge.outputNode == this)
        {
            if (edge.inputNode is not StateNode targetNode || targetNode.stateSO == null) return;
            if (graph is StateMachineGraph sg) sg.targetStateMachine.initialState = targetNode.stateSO;
        }
        base.OnEdgeConnected(edge);
    }

    public override void OnEdgeDisconnected(SerializableEdge edge)
    {
        if (edge.outputNode == this)
        {
            if (graph is StateMachineGraph sg) sg.targetStateMachine.initialState = null;
        }
        base.OnEdgeDisconnected(edge);
    }
}