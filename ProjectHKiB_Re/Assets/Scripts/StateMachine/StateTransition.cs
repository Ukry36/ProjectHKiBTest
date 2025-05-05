[System.Serializable]
public class StateTransition
{
    [System.Serializable]
    public struct DecisionSet
    {
        public StateDecisionSO decision;
        public bool negate;
    }
    public float availableTime;
    public DecisionSet[] decisions;
    public StateSO trueState;
    public StateSO falseState;
}
