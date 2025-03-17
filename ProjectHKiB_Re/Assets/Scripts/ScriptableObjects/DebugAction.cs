using UnityEngine;
[CreateAssetMenu(fileName = "Debug Action", menuName = "Scriptable Objects/State Machine/Action/Debug", order = 3)]
public class DebugAction : StateActionSO
{
    public string str;
    public override void Act(StateController stateController)
    {
        Debug.Log(str);
    }
}
