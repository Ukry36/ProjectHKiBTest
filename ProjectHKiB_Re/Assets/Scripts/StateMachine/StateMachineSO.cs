using UnityEngine;

[CreateAssetMenu(fileName = "State Machine", menuName = "State Machine/State Machine")]
public class StateMachineSO : ScriptableObject
{
    public CustomVariableSets customVariables;
    public StateSO initialState;
}
