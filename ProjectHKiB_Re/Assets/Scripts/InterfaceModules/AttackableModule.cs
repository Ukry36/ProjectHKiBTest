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
    public bool IsAttackCooltime { get; set; }
    public int AttackNumber { get; set; }
    public Transform CurrentTarget { get; set; }
    public Cooltime attackCooltime;

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IAttackable>(this);
    }

    public virtual void Initialize()
    {
        ATKBuffer = new(BaseATK);
        SetAttacker();
        attackCooltime = new();
        IsAttackCooltime = false;
    }

    public void SetAttacker() => damager.SetAttackable(this);

    public void StartAttackCooltime()
    {
        IsAttackCooltime = true;
        attackCooltime.StartCooltime(AttackDatas[AttackNumber].coolTime, () => IsAttackCooltime = false);
    }

    public void SetAttackData(int attackNumber)
    {
        Debug.Log($"[Attack] SetAttackData called with attackNumber: {attackNumber}");
        if (AttackDatas == null)
        {
            Debug.LogError("ERROR: AttackDatas is missing!!!"); return;
        }

        DamageIndicatorRandomPosInfo = Random.value;

        if (attackNumber < AttackDatas.Length)
            AttackNumber = attackNumber;
        else
            AttackNumber = 0;
        Debug.Log($"[Attack] AttackNumber set to: {AttackNumber}");
    }

    public void Attack(int damageNumber)
    {
        Debug.Log($"[Attack] Attack called with damageNumber: {damageNumber}, AttackNumber: {AttackNumber}");
        if (!damager)
        {
            Debug.LogError("ERROR: Damager is missing!!!"); return;
        }
        damager.SetDamageData(AttackNumber, damageNumber);
        Debug.Log("[Attack] Calling damager.Damage()");
        damager.Damage();
    }
}