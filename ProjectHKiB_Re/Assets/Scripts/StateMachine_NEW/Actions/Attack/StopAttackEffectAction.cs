using UnityEngine;
namespace StateMachine
{
    [System.Serializable]
    public class StopAttackEffectAction : StateAction
    {
        public int EffectPlayerNumber;
        public override void Act(StateController stateController)
        {
            if (stateController.TryGetInterface(out IAttackable attackable))
            {
                attackable.StopEffect(EffectPlayerNumber);
            }
            else
                Debug.LogError("ERROR: Interface Not Found!!!");
        }
    }
}