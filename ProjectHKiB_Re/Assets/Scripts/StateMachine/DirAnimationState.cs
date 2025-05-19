using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "Scriptable Objects/State Machine/DirAnimationState")]
public class DirAnimationState : StateSO
{
    [SerializeField] private string animationName;

    public override void EnterState(StateController stateController)
    {
        base.EnterState(stateController);
        if (stateController.TryGetInterface(out IDirAnimatable animatable))
            animatable.AnimationController.Play(animationName);
        else Debug.LogError("ERROR: Interface Not Found!!!");
    }
}