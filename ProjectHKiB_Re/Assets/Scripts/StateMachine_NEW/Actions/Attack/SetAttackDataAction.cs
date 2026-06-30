using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class SetAttackDataAction : StateAction
    {
        [SerializeField] private int attackNumber;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IAttackable attackable))
            {
                attackable.SetAttackData(attackNumber);
            }
            else
                Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}