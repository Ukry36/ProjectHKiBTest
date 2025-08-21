using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class SupplyEventModule : InterfaceModule, ISupply
    {
        [field: SerializeField] public int Amount { get; set; }

        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<ISupply>(this);
        }

        public void Supply(Transform target, int amount)
        {
            if (target.TryGetComponent(out IDamagable damagable))
            {
                damagable.Heal(amount);
            }
            else
            {
                Debug.LogError("HealthController is not registered.");
            }
        }
    }
}