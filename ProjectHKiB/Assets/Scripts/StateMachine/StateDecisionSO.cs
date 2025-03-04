using UnityEngine;

[CreateAssetMenu(fileName = "State Decision", menuName = "Scriptable Objects/State Machine/Decision", order = 4)]
public abstract class StateDecisionSO : ScriptableObject
{
    public abstract bool Decide(StateController stateController);
}