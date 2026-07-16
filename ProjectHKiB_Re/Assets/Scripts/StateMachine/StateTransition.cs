using StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class StateTransition
{
    public string name;
    [System.Serializable]
    public struct DecisionSet
    {
        [SerializeReference, SubclassSelector] public StateDecision Decision;
        public bool negate;
    }

    public EnumManager.InputType activationInput = EnumManager.InputType.None;
    public InputActionReference trigger;
    public EnumManager.InputActionType type;
    public float availableTime;
    public float disableTime;
    public DecisionSet[] decisions;
    public StateSO trueState;
    public StateSO falseState;
    [SerializeReference, SubclassSelector] public StateAction Action;

    public bool showTrueStatePort = true;
    public bool showFalseStatePort = true;

    public bool CheckDecisions(StateController stateController)
    {
        for (int j = 0; j < decisions.Length; j++)
        {
            if (!decisions[j].Decision.Decide(stateController) ^ decisions[j].negate)
                return false;
        }
        return true;
    }
}