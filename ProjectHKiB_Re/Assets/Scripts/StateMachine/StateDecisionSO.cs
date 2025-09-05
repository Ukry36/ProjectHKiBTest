using UnityEngine;

[CreateAssetMenu(fileName = "State Decision", menuName = "State Machine/Decision")]
public abstract class StateDecisionSO : ScriptableObject
{
    public abstract bool Decide(StateController stateController);
}