namespace StateMachine
{
    [System.Serializable]
    public abstract class StateAction
    {
        public abstract void Act(StateController stateController);
    }
}