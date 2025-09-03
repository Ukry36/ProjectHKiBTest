using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class GetBuffEventModule : InterfaceModule, IGetBuff
    {
        [field: SerializeField] public StatBuffSO Buff { get; set; }
        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IGetBuff>(this);
        }
        
        public void GetBuff(Transform target, StatBuffSO buff)
        {
            if (target.TryGetComponent(out IBuffable buffable))
            {
                buffable.StatBuffController.Buff(buff, 1, -1);
            }
            else
            {
                Debug.LogError("BuffController is not registered.");
            }
        }
    }
}