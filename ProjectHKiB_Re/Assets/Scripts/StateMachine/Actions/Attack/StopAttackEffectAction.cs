using UnityEngine;
[CreateAssetMenu(fileName = "StopAttackEffect", menuName = "State Machine/Action/Attack/StopAttackEffect")]
public class StopAttackEffectAction : StateActionSO
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