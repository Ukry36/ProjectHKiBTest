using UnityEngine;
[CreateAssetMenu(fileName = "DamageAction", menuName = "Scriptable Objects/State Machine/Action/Attack/Damage")]
public class DamageAction : StateActionSO
{
    public int damageNumber;
    public override void Act(StateController stateController)
    {
        if (stateController.TryGetInterface(out IAttackable attackable))
        {
            attackable.AttackController.Attack(damageNumber);
        }
        else
            Debug.LogError("ERROR: Interface Not Found!!!");
    }
}