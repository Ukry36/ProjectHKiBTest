using UnityEngine;
[CreateAssetMenu(fileName = "RandomDecision", menuName = "Scriptable Objects/State Machine/Decision/Random", order = 4)]
public class RandomDecision : StateDecisionSO
{
    [Range(0, 1)][SerializeField] private float probability;
    public override bool Decide(StateController stateController)
    {
        return Random.value < probability;
    }
}
