using UnityEngine;

public abstract class SaveBaseStateSO : StateSO
{
    public abstract void OnEnter(SaveModule module);
    public abstract void OnUpdate(SaveModule module);
    public abstract void OnExit(SaveModule module);

    public override void EnterState(StateController stateController)
    {
        base.EnterState(stateController);
        OnEnter(stateController.GetInterface<SaveModule>());
    }

    public override void UpdateState(StateController stateController)
    {
        base.UpdateState(stateController);
        OnUpdate(stateController.GetInterface<SaveModule>());
    }

    public override void ExitState(StateController stateController)
    {
        base.ExitState(stateController);
        OnExit(stateController.GetInterface<SaveModule>());
    }
}