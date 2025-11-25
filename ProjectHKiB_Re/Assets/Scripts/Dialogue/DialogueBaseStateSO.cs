using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DialogueBaseStateSO : StateSO
{
    public abstract void OnEnter(DialogueModule module);
    public abstract void OnUpdate(DialogueModule module);
    public abstract void OnExit(DialogueModule module);

    public override void EnterState(StateController stateController)
    {
        base.EnterState(stateController);
        OnEnter(stateController.GetInterface<DialogueModule>());
    }

    public override void UpdateState(StateController stateController)
    {
        base.UpdateState(stateController);
        OnUpdate(stateController.GetInterface<DialogueModule>());
    }

    public override void ExitState(StateController stateController)
    {
        base.ExitState(stateController);
        OnExit(stateController.GetInterface<DialogueModule>());
    }
}
