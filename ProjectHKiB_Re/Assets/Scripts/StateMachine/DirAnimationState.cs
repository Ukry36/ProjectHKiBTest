using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "State Machine/DirAnimationState")]
public class DirAnimationState : StateSO
{
    [SerializeField] private string animationName;

    public override void EnterState(StateController stateController)
    {
        base.EnterState(stateController);
        if (stateController.TryGetInterface(out IDirAnimatable animatable))
            animatable.Play(animationName);
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}