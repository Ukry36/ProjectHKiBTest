using System;
using Gameplay;
using UnityEngine;

namespace StateMachine
{
    /// <summary>
    /// 에어본이나 퍼즐 행동처럼 자동 연결되지 않은 사건을 현재 Controller에서 발행한다.
    /// 고급 Source·Target 지정이 필요하면 GameplayEventDispatcher를 코드에서 직접 사용한다.
    /// </summary>
    [AddTypeMenu("General/Emit Gameplay Event")]
    [Serializable]
    public sealed class EmitGameplayEventAction : StateAction
    {
        [Tooltip("발행할 공통 게임플레이 사건 종류.")]
        [SerializeField]
        private GameplayEventType _eventType = GameplayEventType.Custom;

        [Tooltip("같은 종류 안에서 구체 행동을 구분할 선택 ID. 예: Airborne.Heavy.")]
        [SerializeField]
        private string _eventId;

        [Tooltip("필요할 때 사건의 원인이 된 ScriptableObject나 GameEvent를 연결한다.")]
        [SerializeField]
        private UnityEngine.Object _definition;

        [Tooltip("선택적으로 전달할 수치. 의미는 사건 종류별 사용처가 정한다.")]
        [SerializeField]
        private float _value;

        /// <summary>
        /// Action을 실행한 Controller를 Source로 삼아 현재 State 스냅샷과 함께 발행한다.
        /// TargetControl의 중첩 Action으로 실행하면 보조 오브젝트의 Root Owner도 자동 복원된다.
        /// </summary>
        public override void Act(StateController stateController)
        {
            if (stateController == null) return;
            GameplayEventDispatcher.Publish(
                _eventType,
                stateController,
                null,
                _eventId,
                _definition,
                stateController.gameObject,
                null,
                _value);
        }
    }
}
