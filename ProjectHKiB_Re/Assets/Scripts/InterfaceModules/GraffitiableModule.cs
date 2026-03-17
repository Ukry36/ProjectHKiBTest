using UnityEngine;
namespace Assets.Scripts.Interfaces.Modules
{
    public class GraffitiableModule : InterfaceModule, IGraffitiable
    {
        [field: NaughtyAttributes.ReadOnly][field: SerializeField] public StateSO GraffitiAttackState { get; set; }
        [field: NaughtyAttributes.ReadOnly][field: SerializeField] public StateSO GraffitiSkillState { get; set; }
        [field: NaughtyAttributes.ReadOnly][field: SerializeField] public Vector2 GraffitiTinkerOffset { get; set; }

        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IGraffitiable>(this);
        }

        public void Initialize()
        {

        }
    }
}