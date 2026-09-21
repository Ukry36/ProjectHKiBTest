using System;
using Gameplay;
using UnityEngine;

namespace Combat
{
    /// <summary>
    /// StateMachine이 패링 판정 창을 열고 닫기 위해 사용하는 최소 계약이다.
    /// 시간과 방향 같은 추가 규칙은 State 구성에 맡겨 설정 항목을 줄인다.
    /// </summary>
    public interface IParryable : IInitializable
    {
        bool IsParryActive { get; }
        event Action<GameplayEvent> Parried;
        void SetParryActive(bool active);
        bool TryParry(in CombatHitRequest request, out GameplayEvent parryEvent);
    }

    /// <summary>
    /// 자신을 소유한 StateController의 현재 패링 창만 관리한다.
    /// 실제 피해 차단과 이벤트 전달은 Hit Receiver가 담당한다.
    /// </summary>
    public sealed class ParryModule : InterfaceModule, IParryable
    {
        [Tooltip("현재 이 StateController가 공격을 패링할 수 있는지 표시한다.")]
        [NaughtyAttributes.ReadOnly, SerializeField]
        private bool _isParryActive;

        [Tooltip("패링 순간의 State 정보를 제공하는 소유 StateController.")]
        [NaughtyAttributes.ReadOnly, SerializeField]
        private StateController _owner;

        public bool IsParryActive => _isParryActive;
        public event Action<GameplayEvent> Parried;

        /// <summary>
        /// StateController의 인터페이스 저장소에 패링 기능을 등록한다.
        /// 같은 오브젝트의 StateAction이 별도 컴포넌트 탐색 없이 접근한다.
        /// </summary>
        public override void Register(IInterfaceRegistable interfaceRegistable)
        {
            interfaceRegistable.RegisterInterface<IParryable>(this);
            _owner = interfaceRegistable as StateController;
        }

        /// <summary>
        /// 재초기화 시 이전 State에서 남은 패링 창을 닫는다.
        /// 소유 Controller가 아직 없으면 같은 오브젝트에서 다시 찾는다.
        /// </summary>
        public void Initialize()
        {
            if (_owner == null) _owner = GetComponent<StateController>();
            _isParryActive = false;
        }

        /// <summary>
        /// State의 Enter·Exit Action이 패링 가능 여부를 명시적으로 지정한다.
        /// 별도 Timer를 두지 않아 패링 시간의 단일 원천을 StateMachine으로 유지한다.
        /// </summary>
        public void SetParryActive(bool active)
        {
            _isParryActive = active;
        }

        /// <summary>
        /// 패링 창과 DamageData의 허용 설정을 확인하고 패링 이벤트를 만든다.
        /// 성공 시 대미지·넉백 적용 전 호출자가 처리를 종료해야 한다.
        /// </summary>
        public bool TryParry(in CombatHitRequest request, out GameplayEvent parryEvent)
        {
            if (!_isParryActive || request.DamageData == null ||
                !request.DamageData.CanBeParried || _owner == null)
            {
                parryEvent = default;
                return false;
            }

            parryEvent = CombatEventDispatcher.PublishParry(request, _owner);
            Parried?.Invoke(parryEvent);
            return true;
        }

        /// <summary>
        /// 풀 반환이나 비활성화 중 이전 패링 창이 남지 않도록 즉시 닫는다.
        /// 다시 활성화될 때는 State의 Enter Action이 명시적으로 열어야 한다.
        /// </summary>
        private void OnDisable()
        {
            _isParryActive = false;
        }
    }

}
