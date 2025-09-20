using System.Collections;
using UnityEngine;

public class AttackableModule : InterfaceModule, IAttackable
{
    public int BaseATK { get; set; }
    public FloatBuffContainer ATKBuffer { get; set; }

    public float CriticalChanceRate { get; set; }
    public float CriticalDamageRate { get; set; }
    public AttackDataSO[] AttackDatas { get; set; }
    public DamageParticleDataSO DamageParticle { get; set; }
    public float DamageIndicatorRandomPosInfo { get; set; }

    [SerializeField] private Damager damager;
    [HideInInspector] public bool isAttackCooltime = false;
    public int AttackNumber { get; private set; }
    public Transform CurrentTarget { get; set; }

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IAttackable>(this);
        ATKBuffer = new(BaseATK);
    }

    public void SetAttacker()
    {
        damager.SetAttackable(this);
    }

    public IEnumerator AttackCooltimeCoroutine()
    {
        isAttackCooltime = true;
        yield return new WaitForSeconds(AttackDatas[AttackNumber].coolTime);
        isAttackCooltime = false;
    }

    public void SetAttackData(int attackNumber)
    {
        if (AttackDatas == null)
        {
            Debug.LogError("ERROR: AttackDatas is missing!!!"); return;
        }

        DamageIndicatorRandomPosInfo = UnityEngine.Random.value;

        if (attackNumber < AttackDatas.Length)
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