using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "State Machine", menuName = "State Machine/State Machine")]
public class StateMachineSO : ScriptableObject
{
    public CustomVariableSets customVariables;
    public StateSO initialState;

    [NaughtyAttributes.Expandable] public StateSO[] allStates;

    public List<CommandPair> _commandPairs;
    [NaughtyAttributes.Button]
    public void UpdateStateMachine()
    {
        _commandPairs = new();
        foreach (StateSO state in allStates)
        {
            state.temporaryID = Random.value;
            foreach(StateTransition transition in state.transitions)
            {
                if (transition.activationInput != EnumManager.InputType.None) 
                    _commandPairs.Add(new(state, transition.activationInput));
            }
        }
    }

    public void BindCommands(StateController stateController)
    {
        for (int i = 0; i < _commandPairs.Count; i++)
            _commandPairs[i].Bind(stateController);
    }

    public void UnbindCommands()
    {
        for (int i = 0; i < _commandPairs.Count; i++)
            _commandPairs[i].Unbind();
    }

}
