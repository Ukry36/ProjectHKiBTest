using System;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Boss SpecialAction으로 관찰할 수 있는 공통 게임플레이 사건 종류다.
    /// 구체적인 행동 이름은 EventId로 분리하여 enum 증가를 억제한다.
    /// </summary>
    public enum GameplayEventType
    {
        Parry,
        Hit,
        Knockback,
        Airborne,
        Interaction,
        Custom
    }

    /// <summary>
    /// 사건 순간의 실제 Source·Target과 각 Root Owner의 State를 함께 보관한다.
    /// 전투와 비전투 시스템이 Boss 코드에 의존하지 않고 같은 정보를 전달한다.
    /// </summary>
    public readonly struct GameplayEvent
    {
        public long Sequence { get; }
        public GameplayEventType Type { get; }
        public string EventId { get; }
        public UnityEngine.Object Definition { get; }
        public StateController SourceController { get; }
        public StateController SourceOwnerController { get; }
        public StateController TargetController { get; }
        public StateController TargetOwnerController { get; }
        public StateSO SourceStateAtStart { get; }
        public StateSO SourceOwnerStateAtStart { get; }
        public StateSO SourceStateAtEvent { get; }
        public StateSO SourceOwnerStateAtEvent { get; }
        public StateSO TargetStateAtEvent { get; }
        public StateSO TargetOwnerStateAtEvent { get; }
        public GameObject SourceObject { get; }
        public GameObject TargetObject { get; }
        public float Value { get; }

        /// <summary>
        /// Dispatcher가 확정한 참여자, State 스냅샷과 선택 정보를 불변 사건으로 만든다.
        /// Start State가 불필요한 사건은 null을 사용한다.
        /// </summary>
        internal GameplayEvent(
            long sequence,
            GameplayEventType type,
            string eventId,
            UnityEngine.Object definition,
            StateController sourceController,
            StateController sourceOwnerController,
            StateController targetController,
            StateController targetOwnerController,
            StateSO sourceStateAtStart,
            StateSO sourceOwnerStateAtStart,
            GameObject sourceObject,
            GameObject targetObject,
            float value)
        {
            Sequence = sequence;
            Type = type;
            EventId = string.IsNullOrWhiteSpace(eventId) ? string.Empty : eventId.Trim();
            Definition = definition;
            SourceController = sourceController;
            SourceOwnerController = sourceOwnerController;
            TargetController = targetController;
            TargetOwnerController = targetOwnerController;
            SourceStateAtStart = sourceStateAtStart;
            SourceOwnerStateAtStart = sourceOwnerStateAtStart;
            SourceStateAtEvent = sourceController != null ? sourceController.CurrentState : null;
            SourceOwnerStateAtEvent = sourceOwnerController != null
                ? sourceOwnerController.CurrentState
                : null;
            TargetStateAtEvent = targetController != null ? targetController.CurrentState : null;
            TargetOwnerStateAtEvent = targetOwnerController != null
                ? targetOwnerController.CurrentState
                : null;
            SourceObject = sourceObject;
            TargetObject = targetObject;
            Value = value;
        }
    }

    /// <summary>
    /// 공통 게임플레이 사건을 받아 보관하거나 즉시 반응할 객체의 계약이다.
    /// Dispatcher는 Source·Target의 실제 Controller와 Root Owner에 사건을 전달한다.
    /// </summary>
    public interface IGameplayEventReceiver
    {
        /// <summary>
        /// Dispatcher가 확정한 하나의 사건을 수신한다.
        /// 구현체는 Sequence로 중복 전달을 구분할 수 있다.
        /// </summary>
        void ReceiveGameplayEvent(GameplayEvent gameplayEvent);
    }

    /// <summary>
    /// StateAction이 소비자별 순서로 게임플레이 사건을 읽기 위한 계약이다.
    /// 읽기 위치는 런타임 객체가 소유하여 공유 StateSO에 상태를 저장하지 않는다.
    /// </summary>
    public interface IGameplayEventReader
    {
        /// <summary>
        /// 소비자 키가 아직 읽지 않은 다음 사건을 반환한다.
        /// 여러 Action은 서로 다른 키로 독립적인 읽기 위치를 갖는다.
        /// </summary>
        bool TryConsumeGameplayEvent(string consumerKey, out GameplayEvent gameplayEvent);
    }

    /// <summary>
    /// 모든 게임플레이 사건에 Sequence를 부여하고 관련 Controller와 전역 관찰자에게 전달한다.
    /// 전역 알림은 보스와 직접 관련 없는 퍼즐 상호작용을 관찰할 때만 사용한다.
    /// </summary>
    public static class GameplayEventDispatcher
    {
        [Tooltip("현재 플레이 세션에서 다음 사건에 부여할 단조 증가 Sequence.")]
        private static long _nextSequence;
        public static event Action<GameplayEvent> Published;

        /// <summary>
        /// Source·Target의 Root Owner를 해석하고 현재 State를 캡처해 사건을 발행한다.
        /// 선택 인자는 전투 공격의 시작 State처럼 이미 캡처한 문맥에만 사용한다.
        /// </summary>
        public static GameplayEvent Publish(
            GameplayEventType type,
            StateController sourceController,
            StateController targetController,
            string eventId = "",
            UnityEngine.Object definition = null,
            GameObject sourceObject = null,
            GameObject targetObject = null,
            float value = 0f,
            StateController sourceOwnerOverride = null,
            StateController targetOwnerOverride = null,
            StateSO sourceStateAtStart = null,
            StateSO sourceOwnerStateAtStart = null)
        {
            StateController sourceOwner = sourceOwnerOverride != null
                ? sourceOwnerOverride
                : CombatOwnershipUtility.ResolveRootOwner(sourceController);
            StateController targetOwner = targetOwnerOverride != null
                ? targetOwnerOverride
                : CombatOwnershipUtility.ResolveRootOwner(targetController);

            GameplayEvent gameplayEvent = new(
                ++_nextSequence,
                type,
                eventId,
                definition,
                sourceController,
                sourceOwner,
                targetController,
                targetOwner,
                sourceStateAtStart,
                sourceOwnerStateAtStart,
                sourceObject,
                targetObject,
                value);
            Dispatch(gameplayEvent);
            Published?.Invoke(gameplayEvent);
            return gameplayEvent;
        }

        /// <summary>
        /// Domain Reload를 끈 Editor 재생과 Scene 전환에서도 이전 구독을 남기지 않는다.
        /// 새 플레이 세션은 사건 Sequence도 처음부터 시작한다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            _nextSequence = 0;
            Published = null;
        }

        /// <summary>
        /// 사건 참여 Controller 네 곳에 중복 없이 같은 스냅샷을 전달한다.
        /// Receiver가 없는 일반 엔티티에는 추가 설정이나 비용을 요구하지 않는다.
        /// </summary>
        private static void Dispatch(GameplayEvent gameplayEvent)
        {
            StateController[] recipients =
            {
                gameplayEvent.SourceController,
                gameplayEvent.SourceOwnerController,
                gameplayEvent.TargetController,
                gameplayEvent.TargetOwnerController
            };

            for (int i = 0; i < recipients.Length; i++)
            {
                StateController recipient = recipients[i];
                if (recipient == null || WasAlreadyDelivered(recipients, i, recipient)) continue;
                IGameplayEventReceiver receiver = recipient.GetComponent<IGameplayEventReceiver>();
                receiver?.ReceiveGameplayEvent(gameplayEvent);
            }
        }

        /// <summary>
        /// 현재 인덱스 앞에서 같은 Controller가 이미 처리됐는지 검사한다.
        /// 고정 크기 배열 순회로 사건마다 별도 HashSet 할당을 피한다.
        /// </summary>
        private static bool WasAlreadyDelivered(
            StateController[] recipients,
            int currentIndex,
            StateController candidate)
        {
            for (int i = 0; i < currentIndex; i++)
                if (recipients[i] == candidate) return true;
            return false;
        }
    }
}
