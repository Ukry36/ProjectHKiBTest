using System;
using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "Scriptable Objects/State Machine/State", order = 2)]
public class StateSO : ScriptableObject
{
    public string animationName;
    public bool directionDependent = true;
    //public string animationBoolName;
    public StateTransition[] transitions;
    public StateActionSO[] enterActions;
    public StateActionSO[] updateActions;
    public StateActionSO[] exitActions;

    public void EnterState(StateController stateController)
    {
        stateController.PlayStateAnimation(animationName, directionDependent);
        for (int i = 0; i < enterActions.Length; i++)
        {
            enterActions[i].Act(stateController);
        }
    }

    public void UpdateState(StateController stateController)
    {
        for (int i = 0; i < updateActions.Length; i++)
        {
            updateActions[i].Act(stateController);
        }
    }
    public void ExitState(StateController stateController)
    {
        for (int i = 0; i < exitActions.Length; i++)
        {
            exitActions[i].Act(stateController);
        }
    }

    public void CheckTransition(StateController stateController)
    {
        for (int i = 0; i < transitions.Length; i++)
        {
            if (transitions[i].decision.Decide(stateController))
                stateController.ChangeState(transitions[i].trueState);
            else
                if (transitions[i].falseState) stateController.ChangeState(transitions[i].falseState);
        }
    }
}
