using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class RandomDecision : StateDecision
    {
        [Range(0, 1)][SerializeField] private float probability;
        public override bool Decide(StateController stateController)
        {
            return Random.value < probability;
        }
    }
}