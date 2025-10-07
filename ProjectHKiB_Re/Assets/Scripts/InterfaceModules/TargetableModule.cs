
using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class TargetableModule : InterfaceModule, ITargetable
    {
        [field: SerializeField][field: NaughtyAttributes.ReadOnly] public Transform CurrentTarget { get; set; }
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