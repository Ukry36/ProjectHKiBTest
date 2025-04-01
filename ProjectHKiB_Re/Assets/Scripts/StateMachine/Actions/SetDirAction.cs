using UnityEngine;
[CreateAssetMenu(fileName = "Set Dir Action", menuName = "Scriptable Objects/State Machine/Action/Set Dir", order = 3)]
public class SetDirAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        stateController.SetAnimationDirection(stateController.velocity);
    }
}
