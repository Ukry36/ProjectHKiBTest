using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "State Machine/AnimationState")]
public class AnimationState : StateSO
{
    [SerializeField] private string animationName;

    public override void EnterState(StateController stateController)
    {
        base.EnterState(stateController);
        if (stateController.TryGetInterface(out IAnimatable animatable))
            animatable.Play(animationName);
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}