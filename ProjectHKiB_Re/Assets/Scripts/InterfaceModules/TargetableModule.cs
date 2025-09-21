
using System;
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class TargetableModule : InterfaceModule, ITargetable
    {
        public Transform CurrentTarget { get; set; }
        public LayerMask[] TargetLayers { get; set; }

        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<ITargetable>(this);
        }

        public void Initialize()
        {
            CurrentTarget = null;
        }
    }
}