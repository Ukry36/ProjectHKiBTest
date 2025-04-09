using System.Collections;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class Damager : MonoBehaviour
{
    [SerializeField] private DamageDataSO _damageData;
    private IAttackable _attackable;
    private BoxCollider2D _damageCollider;
    [SerializeField] private EnumManager.AnimDir _animationDirection;

    private readonly WaitForSeconds damageTime = new(0.05f);
    private void Awake()
    {
        if (TryGetComponent(out BoxCollider2D component))
        {
            _damageCollider = component;
        }
        else Debug.LogError("ERROR: Collider2D is missing in Damager gameObject!!!");
    }

    public void SetDamageDirection(EnumManager.AnimDir animDir)
    => _animationDirection = animDir;

    public void SetAttackable(IAttackable attackable)
    => _attackable = attackable;

    public void SetDamageData(IAttackable attackable, int attackNum, int damageNum)
    {
        SetAttackable(attackable);
        SetDamageData(attackNum, damageNum);
    }

    public void SetDamageData(int attackNum, int damageNum)
    {
        if (_attackable.AttackDatas.Equals(null))
        {
            Debug.LogError("ERROR: AttackDatas is missing!!!"); return;
        }
        if (_attackable.AttackDatas.Length - 1 < attackNum)
        {
            Debug.LogError("ERROR: AttackData[" + attackNum + "] is missing!!!"); return;
        }
        if (_attackable.AttackDatas[attackNum].damageDatas.Length - 1 < damageNum)
        {
            Debug.LogError("ERROR: DamageData[" + damageNum + "] is missing!!!"); return;
        }

        SetDamageData(_attackable.AttackDatas[attackNum].damageDatas[damageNum]);
    }

    public void SetDamageData(DamageDataSO damageData)
    {
        _damageData = damageData;
    }

    public void Damage()
    {
        StopAllCoroutines();
        _damageCollider.size = _damageData.downwardDamageArea.size;
        _damageCollider.offset = _damageData.downwardDamageArea.offset;
        _damageCollider.enabled = true;
        if (_damageData.initialSound)
            GameManager.instance.audioManager.PlayAudioOneShot(_damageData.initialSound, 1, transform.position);
        GameManager.instance.particleManager.PlayParticle(_damageData.DLRUDamageEffects[_animationDirection].GetHashCode(), transform, false);
        this.transform.eulerAngles = _animationDirection switch
        {
            EnumManager.AnimDir.D => Vector3.zero,
            EnumManager.AnimDir.L => Vector3.forward * -90,
            EnumManager.AnimDir.R => Vector3.forward * 90,
            EnumManager.AnimDir.U => Vector3.forward * 180,
            _ => Vector3.zero
        };
        StartCoroutine(DisableDamager());
    }

    public IEnumerator DisableDamager()
    {
        yield return damageTime;
        _damageCollider.enabled = false;
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (_damageData && collision)
            if ((_damageData.damageLayer & (1 << collision.gameObject.layer)) != 0)
            {
                if (collision.TryGetComponent(out IDamagable component))
                {
                    component.Damage(_damageData, _attackable);
                }
            }
    }
}
