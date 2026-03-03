using UnityEngine;
[CreateAssetMenu(fileName = "DamageAction", menuName = "State Machine/Action/Attack/Damage")]
public class DamageAction : StateActionSO
{
    public int damageNumber;
    public override void Act(StateController stateController)
    {
        Debug.Log("[DamageAction] Act() called");
        if (stateController.TryGetInterface(out IAttackable attackable))
        {
            Debug.Log($"[DamageAction] IAttackable found - calling Attack({damageNumber})");
            attackable.Attack(damageNumber);
        }
        else
            Debug.LogError("ERROR: DamageAction - Interface Not Found!!!");
    }
}