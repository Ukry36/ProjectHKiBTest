using UnityEngine;
[CreateAssetMenu(fileName = "SetDirRandomAction", menuName = "Scriptable Objects/State Machine/Action/SetDirRandomAction", order = 3)]
public class SetDirRandomAction : StateActionSO
{
    [SerializeField] private bool negative;
    public override void Act(StateController stateController)
    {
        Vector2 dir = Vector2.up * Random.Range(-1, 2) + Vector2.right * Random.Range(-1, 2);
        if (!stateController.animationController.CheckIfLastSetDirectionSame(dir))
            stateController.animationController.SetAnimationDirection(negative ? dir * -1 : dir);
    }
}
