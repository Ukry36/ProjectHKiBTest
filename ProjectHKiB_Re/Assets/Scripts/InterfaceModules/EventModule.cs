public class EventModule : InterfaceModule, IEvent
{
    public EventTargets CurrentTargets { get; set; }
    public override void Register(IInterfaceRegistable interfaceRegistable)
    {

        interfaceRegistable.RegisterInterface<IEvent>(this);
    }
    public virtual void Initialize() { }

}