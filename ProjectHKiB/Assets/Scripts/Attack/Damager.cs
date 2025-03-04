using UnityEngine;

public class Damager : MonoBehaviour
{
    public DamageDataSO damageData;
    public IAttackable attackable;

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if ((damageData.damageLayer & (1 << collision.gameObject.layer)) != 0)
        {
            if (TryGetComponent(out IDamagable component))
            {
                component.Damage(damageData, attackable, component);
            }
        }
    }
}
