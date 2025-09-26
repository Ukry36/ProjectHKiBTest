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

        public void Initialize()
        {

        }

        public void GetBuff(Transform target, StatBuffSO buff)
        {
            if (target.TryGetComponent(out IBuffable buffable))
            {
                buffable.Buff(buff, 1, -1);
            }
            else
            {
                Debug.LogError("BuffController is not registered.");
            }
        }
    }
}