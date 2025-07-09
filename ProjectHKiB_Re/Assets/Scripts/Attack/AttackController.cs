using System.Collections;
using UnityEngine;

public class AttackController : MonoBehaviour
{
    [SerializeField] private Damager damager;
    private IAttackable _attackable;
    [HideInInspector] public bool isAttackCooltime = false;
    public int AttackNumber { get; private set; }
    public Transform CurrentTarget { get; set; }
    public FloatBuffCalculator ATKBuffer { get; set; } = new();

    public void SetAttacker(IAttackable attackable)
    {
        _attackable = attackable;
        damager.SetAttackable(attackable);
    }

    public IEnumerator AttackCooltimeCoroutine()
    {
        isAttackCooltime = true;
        yield return new WaitForSeconds(_attackable.AttackDatas[AttackNumber].coolTime);
        isAttackCooltime = false;
    }

    public void SetAttackData(int attackNumber)
    {
        if (_attackable == null)
        {
            Debug.LogError("ERROR: Attackable not attatched!!!"); return;
        }
        if (_attackable.AttackDatas == null)
        {
            Debug.LogError("ERROR: AttackDatas is missing!!!"); return;
        }

        _attackable.DamageIndicatorRandomPosInfo = Random.value;

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