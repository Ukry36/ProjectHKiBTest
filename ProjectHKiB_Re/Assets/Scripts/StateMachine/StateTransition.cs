[System.Serializable]
public class StateTransition
{
    [System.Serializable]
    public struct DecisionSet
    {
        public StateDecisionSO decision;
        public bool negate;
    }
    public EnumManager.InputType activationInput = EnumManager.InputType.None;
    public float availableTime;
    public DecisionSet[] decisions;
    public StateSO trueState;
    public StateSO falseState;
    public StateActionSO action;

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
