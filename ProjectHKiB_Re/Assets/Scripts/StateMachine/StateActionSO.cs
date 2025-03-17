using UnityEngine;

[CreateAssetMenu(fileName = "State Action", menuName = "Scriptable Objects/State Machine/Action", order = 3)]
public abstract class StateActionSO : ScriptableObject
{
    public abstract void Act(StateController stateController);
}