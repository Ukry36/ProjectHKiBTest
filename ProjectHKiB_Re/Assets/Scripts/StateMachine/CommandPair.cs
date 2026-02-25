using System;
using UnityEngine.InputSystem;

[Serializable]
public class CommandPair
{
    public CommandPair (StateSO conditionState, EnumManager.InputType triggerInput)
    {
        this.conditionState = conditionState;
        this.triggerInput = triggerInput;
    }
    public StateSO conditionState;
    public EnumManager.InputType triggerInput;

    private Action<InputAction.CallbackContext> _cachedBindFunction;

    public void Bind(StateController stateController)
    {
        if (_cachedBindFunction != null) return;

        _cachedBindFunction = (context) =>
        {
            if (stateController.CurrentState == conditionState)
                stateController.CurrentState.CheckInputDecision(stateController, triggerInput);
        };

        GameManager.instance.inputManager.Bind(triggerInput, _cachedBindFunction);
    }

    public void Unbind()
    {
        GameManager.instance.inputManager.UnBind(triggerInput, _cachedBindFunction);
    }

}