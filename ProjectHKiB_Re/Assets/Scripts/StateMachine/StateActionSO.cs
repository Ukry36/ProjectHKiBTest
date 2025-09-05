using UnityEngine;

[CreateAssetMenu(fileName = "State Action", menuName = "State Machine/Action")]
public abstract class StateActionSO : ScriptableObject
{
    public abstract void Act(StateController stateController);
}