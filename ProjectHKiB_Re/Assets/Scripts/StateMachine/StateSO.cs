using System;
using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "Scriptable Objects/State Machine/State", order = 2)]
public class StateSO : ScriptableObject
{
    [Serializable]
    public struct SubDecision
    {
        public StateDecisionSO decision;
        public StateActionSO action;
    }

    public string animationName;
    public bool directionDependent = true;
    public StateTransition[] transitions;
    public StateActionSO[] enterActions;
    public StateActionSO[] updateActions;
    public StateActionSO[] exitActions;

    public SubDecision[] subDecisions;

    public void EnterState(StateController stateController)
    {
        for (int i = 0; i < enterActions.Length; i++)
        {
            enterActions[i].Act(stateController);
        }
        stateController.PlayStateAnimation(animationName, directionDependent);
    }

    public void UpdateState(StateController stateController)
    {
        for (int i = 0; i < updateActions.Length; i++)
        {
            updateActions[i].Act(stateController);
        }
        for (int i = 0; i < subDecisions.Length; i++)
        {
            if (subDecisions[i].decision.Decide(stateController))
                subDecisions[i].action.Act(stateController);
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
            bool canTransition = true;
            for (int j = 0; j < transitions[i].decisions.Length; j++)
            {
                if (!transitions[i].decisions[j].decision.Decide(stateController) ^ transitions[i].decisions[j].negate)
                {
                    canTransition = false;
                    break;
                }
            }
            if (canTransition)
            {

                string debugMessage = "";
                for (int j = 0; j < transitions[i].decisions.Length; j++)
                {
                    debugMessage += transitions[i].decisions[j].decision.name
                                + ": "
                                + transitions[i].decisions[j].decision.Decide(stateController) + " ";
                }
                Debug.Log(transitions[i].trueState.name + "\n" + debugMessage);

                stateController.ChangeState(transitions[i].trueState);
            }
        }
    }
}
