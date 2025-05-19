using UnityEngine;

[CreateAssetMenu(fileName = "State Machine", menuName = "Scriptable Objects/State Machine/State Machine", order = 1)]
public class StateMachineSO : ScriptableObject
{
    public CustomVariableSets customVariables;
    public EntityStateSO initialState;
}
