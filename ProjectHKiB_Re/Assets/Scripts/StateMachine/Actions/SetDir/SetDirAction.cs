using UnityEngine;
[CreateAssetMenu(fileName = "SetDir", menuName = "State Machine/Action/SetDir/SetDir")]
public class SetDirAction : StateActionSO
{
    [SerializeField] private EnumManager.AnimDir animDir;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IDirAnimatable animatable))
        {
            animatable.SetAnimationDirection(animDir);
        }
    }
}
