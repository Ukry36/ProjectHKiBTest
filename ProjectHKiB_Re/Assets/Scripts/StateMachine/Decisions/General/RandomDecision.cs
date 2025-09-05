using UnityEngine;
[CreateAssetMenu(fileName = "RandomDecision", menuName = "State Machine/Decision/General/Random")]
public class RandomDecision : StateDecisionSO
{
    [Range(0, 1)][SerializeField] private float probability;
    public override bool Decide(StateController stateController)
    {
        return Random.value < probability;
    }
}
