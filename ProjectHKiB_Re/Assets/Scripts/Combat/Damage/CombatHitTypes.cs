using UnityEngine;
using Gameplay;

namespace Combat
{
    /// <summary>
    /// 한 번의 공격 접촉이 최종적으로 어떻게 처리됐는지 나타낸다.
    /// 공격 인스턴스는 이 값으로 일반 피격과 패링을 구분한다.
    /// </summary>
    public enum CombatHitOutcome
    {
        Invalid,
        Ignored,
        Damaged,
        Invulnerable,
        Parried
    }

    /// <summary>
    /// 공격 오브젝트와 그 공격의 실제 소유자를 함께 전달하는 불변 요청이다.
    /// 별도 소환물이 공격해도 Root Owner의 StateMachine 문맥을 잃지 않는다.
    /// </summary>
    public readonly struct CombatHitRequest
    {
        public DamageDataSO DamageData { get; }
        public IAttackable Attacker { get; }
        public StateController AttackSourceController { get; }
        public StateController AttackerController { get; }
        public StateSO AttackSourceStateAtStart { get; }
        public StateSO AttackerStateAtStart { get; }
        public GameObject AttackObject { get; }
        public CombatAttackDefinitionSO AttackDefinition { get; }
        public int AttackHandle { get; }
        public string AttackSlot { get; }
        public Vector3 HitPosition { get; }
        public Vector3 KnockbackOrigin { get; }

        /// <summary>
        /// 공격 시작 시 캡처한 주체 정보와 이번 접촉 위치를 하나의 요청으로 묶는다.
        /// Source는 보조 오브젝트, Attacker는 최종 소유 엔티티를 의미한다.
        /// </summary>
        public CombatHitRequest(
            DamageDataSO damageData,
            IAttackable attacker,
            StateController attackSourceController,
            StateController attackerController,
            StateSO attackSourceStateAtStart,
            StateSO attackerStateAtStart,
            GameObject attackObject,
            CombatAttackDefinitionSO attackDefinition,
            int attackHandle,
            string attackSlot,
            Vector3 hitPosition,
            Vector3 knockbackOrigin)
        {
            DamageData = damageData;
            Attacker = attacker;
            AttackSourceController = attackSourceController;
            AttackerController = attackerController;
            AttackSourceStateAtStart = attackSourceStateAtStart;
            AttackerStateAtStart = attackerStateAtStart;
            AttackObject = attackObject;
            AttackDefinition = attackDefinition;
            AttackHandle = attackHandle;
            AttackSlot = string.IsNullOrWhiteSpace(attackSlot) ? "Legacy" : attackSlot.Trim();
            HitPosition = hitPosition;
            KnockbackOrigin = knockbackOrigin;
        }

        /// <summary>
        /// 기존 Damager 호출을 새 요청 형식으로 안전하게 감싼다.
        /// 별도 소환물이라면 EntityOwnershipModule에서 Root Owner를 복원한다.
        /// </summary>
        public static CombatHitRequest CreateLegacy(
            DamageDataSO damageData,
            IAttackable attacker,
            GameObject attackObject,
            Vector3 hitPosition,
            Vector3 knockbackOrigin)
        {
            StateController source = (attacker as Component)?.GetComponentInParent<StateController>();
            StateController root = CombatOwnershipUtility.ResolveRootOwner(source);
            return new CombatHitRequest(
                damageData,
                attacker,
                source,
                root,
                source != null ? source.CurrentState : null,
                root != null ? root.CurrentState : null,
                attackObject,
                null,
                0,
                CombatOwnershipUtility.ResolveSpawnSlot(source),
                hitPosition,
                knockbackOrigin);
        }
    }

    /// <summary>
    /// 피격 처리 결과와 실제 적용 피해량을 호출자에게 돌려준다.
    /// Parried 결과일 때만 Event가 패링 사건을 가리킨다.
    /// </summary>
    public readonly struct CombatHitResult
    {
        public CombatHitOutcome Outcome { get; }
        public int AppliedDamage { get; }
        public GameplayEvent Event { get; }
        public bool IsResolved => Outcome != CombatHitOutcome.Invalid &&
                                  Outcome != CombatHitOutcome.Ignored;

        /// <summary>
        /// 결과 종류, 피해량과 선택적 패링 이벤트를 불변 값으로 만든다.
        /// 호출자는 IsResolved로 공격 종료 여부를 간단히 판단할 수 있다.
        /// </summary>
        public CombatHitResult(
            CombatHitOutcome outcome,
            int appliedDamage = 0,
            GameplayEvent gameplayEvent = default)
        {
            Outcome = outcome;
            AppliedDamage = Mathf.Max(0, appliedDamage);
            Event = gameplayEvent;
        }
    }

    /// <summary>
    /// HP 보유 여부와 무관하게 공격 접촉을 처리하는 최소 계약이다.
    /// 일반 Damagable과 패링 벽이 같은 공격 판정 경로를 공유한다.
    /// </summary>
    public interface ICombatHitReceiver
    {
        CombatHitResult ReceiveHit(in CombatHitRequest request);
    }

    /// <summary>
    /// Collider에서 새 피격 계약을 찾고 기존 IDamagable을 호환 경로로 지원한다.
    /// 두 공격 구현이 대상 탐색 규칙과 결과 해석을 공유하게 한다.
    /// </summary>
    public static class CombatHitDelivery
    {
        /// <summary>
        /// Collider 자신과 부모에서 새 Receiver를 우선 찾고 요청을 전달한다.
        /// 찾지 못하면 기존 Damage 호출을 사용하여 기존 센서와 에셋을 보존한다.
        /// </summary>
        public static bool TryDeliver(
            Collider2D collider,
            in CombatHitRequest request,
            out CombatHitResult result)
        {
            if (collider == null)
            {
                result = new CombatHitResult(CombatHitOutcome.Invalid);
                return false;
            }

            ICombatHitReceiver receiver = collider.GetComponent<ICombatHitReceiver>() ??
                                          collider.GetComponentInParent<ICombatHitReceiver>();
            if (receiver != null)
            {
                result = receiver.ReceiveHit(request);
                return true;
            }

            IDamagable damageable = collider.GetComponent<IDamagable>() ??
                                    collider.GetComponentInParent<IDamagable>();
            if (damageable != null)
            {
                damageable.Damage(
                    request.DamageData,
                    request.Attacker,
                    request.KnockbackOrigin);
                StateController target = collider.GetComponentInParent<StateController>();
                GameplayEvent hitEvent = CombatEventDispatcher.PublishHit(request, target, 0);
                result = new CombatHitResult(
                    CombatHitOutcome.Damaged,
                    gameplayEvent: hitEvent);
                return true;
            }

            result = new CombatHitResult(CombatHitOutcome.Invalid);
            return false;
        }
    }

    /// <summary>
    /// 전투 결과를 공통 GameplayEvent 형태로 발행하는 얇은 변환 계층이다.
    /// Boss를 포함한 수신자는 전투 구현을 몰라도 사건 종류와 참여자를 검사할 수 있다.
    /// </summary>
    public static class CombatEventDispatcher
    {
        /// <summary>
        /// 공격 요청과 실제 패링 주체를 범용 Parry 사건으로 발행한다.
        /// 공격 정의가 없으면 DamageData를 Definition으로 사용한다.
        /// </summary>
        public static GameplayEvent PublishParry(
            in CombatHitRequest request,
            StateController parrySource)
        {
            Object definition = request.AttackDefinition != null
                ? request.AttackDefinition
                : request.DamageData;
            return GameplayEventDispatcher.Publish(
                GameplayEventType.Parry,
                request.AttackSourceController,
                parrySource,
                request.AttackSlot,
                definition,
                request.AttackObject,
                parrySource != null ? parrySource.gameObject : null,
                sourceOwnerOverride: request.AttackerController,
                sourceStateAtStart: request.AttackSourceStateAtStart,
                sourceOwnerStateAtStart: request.AttackerStateAtStart);
        }

        /// <summary>
        /// 패링되지 않은 정상 공격 접촉을 Hit 사건으로 발행한다.
        /// Value에는 실제 적용 피해량을 넣어 필요할 때 수치 조건을 확장할 수 있다.
        /// </summary>
        public static GameplayEvent PublishHit(
            in CombatHitRequest request,
            StateController target,
            int appliedDamage)
        {
            Object definition = request.AttackDefinition != null
                ? request.AttackDefinition
                : request.DamageData;
            return GameplayEventDispatcher.Publish(
                GameplayEventType.Hit,
                request.AttackSourceController,
                target,
                request.AttackSlot,
                definition,
                request.AttackObject,
                target != null ? target.gameObject : null,
                appliedDamage,
                request.AttackerController,
                sourceStateAtStart: request.AttackSourceStateAtStart,
                sourceOwnerStateAtStart: request.AttackerStateAtStart);
        }

        /// <summary>
        /// 공격으로 실제 넉백이 시작됐음을 별도 Knockback 사건으로 발행한다.
        /// Value에는 DamageData에 설정된 넉백 세기를 기록한다.
        /// </summary>
        public static GameplayEvent PublishKnockback(
            in CombatHitRequest request,
            StateController target)
        {
            Object definition = request.AttackDefinition != null
                ? request.AttackDefinition
                : request.DamageData;
            return GameplayEventDispatcher.Publish(
                GameplayEventType.Knockback,
                request.AttackSourceController,
                target,
                request.AttackSlot,
                definition,
                request.AttackObject,
                target != null ? target.gameObject : null,
                request.DamageData != null ? request.DamageData.knockBack : 0f,
                request.AttackerController,
                sourceStateAtStart: request.AttackSourceStateAtStart,
                sourceOwnerStateAtStart: request.AttackerStateAtStart);
        }
    }
}
