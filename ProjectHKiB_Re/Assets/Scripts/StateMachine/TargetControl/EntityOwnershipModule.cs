using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// 생성된 보조 엔티티가 직접 생성자와 최종 Root Owner를 노출하는 계약이다.
    /// Transform 부모나 파괴 책임과 전투 행동의 소유권을 분리한다.
    /// </summary>
    public interface IEntityOwnership : IInitializable
    {
        StateController DirectOwner { get; }
        StateController RootOwner { get; }
        StateSO RootOwnerStateAtSpawn { get; }
        string SpawnSlot { get; }
        void Bind(StateController directOwner, string spawnSlot);
    }

    /// <summary>
    /// GeneralStateActions가 생성한 StateController에 전투 소유권을 보관한다.
    /// 소환물이 다시 소환물을 만들어도 최초 보스·플레이어까지 한 번에 찾을 수 있다.
    /// </summary>
    public sealed class EntityOwnershipModule : InterfaceModule, IEntityOwnership
    {
        [Tooltip("이 보조 엔티티를 직접 생성한 StateController.")]
        [NaughtyAttributes.ReadOnly, SerializeField]
        private StateController _directOwner;

        [Tooltip("소환 계층 최상단에서 전투 결과를 소유할 보스 또는 플레이어 StateController.")]
        [NaughtyAttributes.ReadOnly, SerializeField]
        private StateController _rootOwner;

        [Tooltip("이 엔티티가 생성될 당시 Root Owner가 실행하던 State.")]
        [NaughtyAttributes.ReadOnly, SerializeField]
        private StateSO _rootOwnerStateAtSpawn;

        [Tooltip("생성 Action의 Target Control 슬롯 이름.")]
        [NaughtyAttributes.ReadOnly, SerializeField]
        private string _spawnSlot = "Default";

        public StateController DirectOwner => _directOwner;
        public StateController RootOwner => _rootOwner;
        public StateSO RootOwnerStateAtSpawn => _rootOwnerStateAtSpawn;
        public string SpawnSlot => _spawnSlot;

        /// <summary>
        /// 소유권 조회가 StateController의 공통 인터페이스 저장소를 통하도록 등록한다.
        /// 생성 직후 Bind되기 전에도 Module 자체는 안전하게 조회할 수 있다.
        /// </summary>
        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IEntityOwnership>(this);
        }

        /// <summary>
        /// 별도 런타임 컨테이너가 없어 초기화 시 추가 작업을 하지 않는다.
        /// 실제 소유 정보는 생성한 TargetControlModule이 Bind로 주입한다.
        /// </summary>
        public void Initialize() { }

        /// <summary>
        /// 직접 생성자에서 Root Owner를 해석하고 생성 시점 State를 캡처한다.
        /// Transform 자식 여부와 관계없이 동일한 전투 소유권을 유지한다.
        /// </summary>
        public void Bind(StateController directOwner, string spawnSlot)
        {
            _directOwner = directOwner;
            _rootOwner = CombatOwnershipUtility.ResolveRootOwner(directOwner);
            _rootOwnerStateAtSpawn = _rootOwner != null ? _rootOwner.CurrentState : null;
            _spawnSlot = string.IsNullOrWhiteSpace(spawnSlot) ? "Default" : spawnSlot.Trim();
        }
    }

    /// <summary>
    /// StateController의 선택적 EntityOwnershipModule을 일관되게 해석한다.
    /// 소유권 Module이 없는 일반 엔티티는 자기 자신을 Root Owner로 취급한다.
    /// </summary>
    public static class CombatOwnershipUtility
    {
        /// <summary>
        /// 연결된 Root Owner가 살아 있으면 반환하고 아니면 입력 Controller를 반환한다.
        /// 루트 엔티티에는 별도 Ownership 설정이 필요하지 않다.
        /// </summary>
        public static StateController ResolveRootOwner(StateController controller)
        {
            if (controller == null) return null;
            if (controller.TryGetInterface(out IEntityOwnership ownership) &&
                ownership.RootOwner != null)
                return ownership.RootOwner;

            EntityOwnershipModule module = controller.GetComponent<EntityOwnershipModule>();
            return module != null && module.RootOwner != null ? module.RootOwner : controller;
        }

        /// <summary>
        /// 생성 시 부여한 슬롯을 반환하여 SpecialAction이 소환물 종류를 구분하게 한다.
        /// 일반 루트 엔티티나 미연결 객체는 빈 문자열을 반환한다.
        /// </summary>
        public static string ResolveSpawnSlot(StateController controller)
        {
            if (controller == null) return string.Empty;
            if (controller.TryGetInterface(out IEntityOwnership ownership))
                return ownership.SpawnSlot;

            EntityOwnershipModule module = controller.GetComponent<EntityOwnershipModule>();
            return module != null ? module.SpawnSlot : string.Empty;
        }
    }
}
