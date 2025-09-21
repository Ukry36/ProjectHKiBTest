using UnityEngine;
[CreateAssetMenu(fileName = "SetDirRandomAction", menuName = "State Machine/Action/SetDir/SetDirRandomAction")]
public class SetDirRandomAction : StateActionSO
{
    [SerializeField] private bool negative;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDirAnimatable animatable))
        {
            Vector2 dir = Vector2.up * Random.Range(-1, 2) + Vector2.right * Random.Range(-1, 2);
            if (!animatable.CheckIfLastSetDirectionSame(dir))
                animatable.SetAnimationDirection(negative ? dir * -1 : dir);
        }

    }
}
