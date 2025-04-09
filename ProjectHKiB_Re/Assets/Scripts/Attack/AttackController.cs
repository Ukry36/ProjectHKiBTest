using UnityEngine;

public class AttackController : MonoBehaviour
{
    [SerializeField] private Damager damager;
    private IAttackable _attackable;
    public int AttackNumber { get; private set; }

    public void SetAttacker(IAttackable attackable)
    {
        _attackable = attackable;
        damager.SetAttackable(attackable);
    }

    public void SetAttackData(int attackNumber)
    {
        if (_attackable.Equals(null))
        {
            Debug.LogError("ERROR: Attackable not attatched!!!"); return;
        }
        if (_attackable.AttackDatas.Equals(null))
        {
            Debug.LogError("ERROR: AttackDatas is missing!!!"); return;
        }

        if (attackNumber < _attackable.AttackDatas.Length)
            AttackNumber = attackNumber;
        else
            AttackNumber = 0;
    }

    public void Attack(int damageNumber)
    {
        if (!damager)
        {
            Debug.LogError("ERROR: Damager is missing!!!"); return;
        }

        damager.SetDamageData(AttackNumber, damageNumber);
        damager.Damage();
    }
}