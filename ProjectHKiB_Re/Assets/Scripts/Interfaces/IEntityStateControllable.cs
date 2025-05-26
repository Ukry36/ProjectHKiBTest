public interface IEntityStateControllable : IDirAnimatable
{
    public StateMachineSO StateMachine { get; set; }
    public StateController StateController { get; set; }
}