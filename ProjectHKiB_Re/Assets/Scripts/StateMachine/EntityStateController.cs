using UnityEngine;

public class EntityStateController : StateController, IEntityStateController
{
    [field: SerializeField] public AnimationController AnimationController { get; set; }

    protected override void Awake()
    {
        base.Awake();
        RegisterInterface<IEntityStateController>(this);
    }
    public override void ChangeState(StateSO state)
    {
        base.ChangeState(state);
    }
}