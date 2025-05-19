using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class TargetableModule : InterfaceModule, ITargetable
    {
        [field: SerializeField] public LayerMask[] TargetLayers { get; set; }
        [field: SerializeField] public Transform CurrentTarget { get; set; }

        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<ITargetable>(this);
        }
    }
}