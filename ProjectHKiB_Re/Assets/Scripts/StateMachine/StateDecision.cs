namespace StateMachine
{
    [System.Serializable]
    public abstract class StateDecision
    {
        public abstract bool Decide(StateController stateController);
    }
}