using UnityEngine;
using Gameplay;

namespace Combat
{
    /// <summary>
    /// HP가 없는 패링 벽이 공통 공격 요청을 받을 수 있게 하는 Receiver다.
    /// 패링에 실패한 공격은 Ignored로 반환하여 공격체가 계속 진행하게 한다.
    /// </summary>
    [RequireComponent(typeof(StateController))]
    [RequireComponent(typeof(ParryModule))]
    public sealed class ParryHitReceiverModule : MonoBehaviour, ICombatHitReceiver
    {
        [Tooltip("패링 상태와 이벤트 주체 정보를 조회할 StateController.")]
        [NaughtyAttributes.ReadOnly, SerializeField]
        private StateController _owner;

        /// <summary>
        /// 같은 오브젝트의 StateController를 캐시하여 매 접촉의 탐색 비용을 줄인다.
        /// 패링 Module 등록은 StateController의 초기화 과정에서 처리된다.
        /// </summary>
        private void Awake()
        {
            _owner = GetComponent<StateController>();
        }

        /// <summary>
        /// 활성 패링이면 Parried 결과를, 아니면 통과 가능한 Ignored 결과를 반환한다.
        /// 이 컴포넌트는 HP·방어력·넉백을 만들지 않는다.
        /// </summary>
        public CombatHitResult ReceiveHit(in CombatHitRequest request)
        {
            if (_owner != null && _owner.TryGetInterface(out IParryable parryable) &&
                parryable.TryParry(request, out GameplayEvent parryEvent))
                return new CombatHitResult(CombatHitOutcome.Parried, gameplayEvent: parryEvent);

            return new CombatHitResult(CombatHitOutcome.Ignored);
        }
    }
}
