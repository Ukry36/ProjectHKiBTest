using UnityEngine;

[CreateAssetMenu(
    fileName = "SelfDamageAction",
    menuName = "State Machine/Action/Attack/SelfDamageAction"
)]
public class SelfDamageAction : StateActionSO
{
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable) &&
            attackable is AttackableModule attackableModule)
        {
            attackableModule.ExecuteSelfDamage();
        }
        else
        {
            Debug.LogError("ERROR: IAttackable not found.");
        }
    }
}