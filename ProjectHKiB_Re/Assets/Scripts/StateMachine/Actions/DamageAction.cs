using UnityEngine;
[CreateAssetMenu(fileName = "DamageAction", menuName = "Scriptable Objects/State Machine/Action/Damage", order = 3)]
public class DamageAction : StateActionSO
{
    public int damageNumber;
    public override void Act(StateController stateController)
    {
        var attackable = stateController.GetInterface<IAttackable>();
        if (attackable != null)
        {
            attackable.AttackController.Attack(damageNumber);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");

    }
}