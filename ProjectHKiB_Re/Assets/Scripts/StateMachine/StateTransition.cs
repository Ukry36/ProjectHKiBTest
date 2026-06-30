using StateMachine;
using UnityEngine;

[System.Serializable]
public class StateTransition
{
    [System.Serializable]
    public struct DecisionSet
    {
        public StateDecisionSO decision;
        [SerializeReference, SubclassSelector] public StateDecision Decision;
        public bool negate;
    }

    public EnumManager.InputType activationInput = EnumManager.InputType.None;
    public float availableTime;
    public float disableTime;
    public DecisionSet[] decisions;
    public StateSO trueState;
    public StateSO falseState;
    public StateActionSO action;
    [SerializeReference, SubclassSelector] public StateAction Action;

    public bool CheckDecisions(StateController stateController)
    {
        for (int j = 0; j < decisions.Length; j++)
        {
            if (!decisions[j].decision.Decide(stateController) ^ decisions[j].negate)
                return false;
        }
        return true;
    }
}