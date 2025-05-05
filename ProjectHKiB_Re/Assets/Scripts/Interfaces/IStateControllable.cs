using UnityEditor.Animations;

public interface IStateControllable
{
    public StateMachineSO StateMachine { get; set; }
    public AnimatorController AnimatorController { get; set; }
    public AnimationController AnimationController { get; set; }
    public StateController StateController { get; set; }
}