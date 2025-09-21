using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class TeleportEventModule : InterfaceModule, ITeleportEventable
    {
        [field: SerializeField] public Transform Destination { get; set; }
        [field: SerializeField] public EnumManager.AnimDir EndDir { get; set; }

        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<ITeleportEventable>(this);
        }
    }
}