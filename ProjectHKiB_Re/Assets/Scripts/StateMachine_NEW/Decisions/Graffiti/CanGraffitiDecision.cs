namespace StateMachine
{
    [System.Serializable]
    public class CanGraffitiDecision : StateDecision
    {
        public override bool Decide(StateController stateController)
        {
            return GameManager.instance.graffitiManager.CanGraffiti;
        }
    }
}