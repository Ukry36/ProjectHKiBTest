# Parry

패링은 무적 Buff가 아니라 대미지와 넉백 적용 전에 확정되는 `CombatHitOutcome.Parried` 결과다. 새 전투 판정의 정본은 `CombatAttackInstance`와 `ICombatHitReceiver`이며, 기존 `Damager`는 이 경로에 연결하지 않는다.

## 일반 엔티티 설정

1. 패링할 `StateController`에 `ParryModule`을 추가한다.
2. 패링 가능한 공격의 `DamageDataSO`에서 `Can Be Parried`를 켠다.
3. 패링 State의 `EnterActions`에 `SetParryWindowAction(Active=true)`를 넣는다.
4. 같은 State의 `ExitActions`에 `SetParryWindowAction(Active=false)`를 넣는다.

패링 시간은 별도 Module 값이 아니라 State 전이 시간으로 관리한다. 패링 성공 시 일반 대미지, 넉백과 피격 이벤트는 실행되지 않는다.

## HP가 없는 패링 벽

패링 벽 Prefab에는 `StateController`, `ParryModule`, `ParryHitReceiverModule`, `Collider2D`를 추가한다. `DamagableModule`은 필요하지 않다. 벽 State에서도 일반 엔티티와 동일하게 `SetParryWindowAction`으로 판정 창을 연다.

`InstanciateObjectAction`으로 생성하면 `EntityOwnershipModule`이 자동 추가되고 Target Control 슬롯과 Root Owner가 주입된다. Transform의 `parentToOwner` 설정은 사건 소유권에 영향을 주지 않는다.

## Boss SpecialAction

Boss Pattern State의 `UpdateActions`에 `ReportBossGameplayEventSpecialAction`을 추가하고 Event Type을 `Parry`로 선택한다. 기존 `ReportBossParrySpecialAction`도 호환용으로 계속 사용할 수 있다.

- `Boss Role`: 보스가 Source(공격 측)인지 Target(패링 측)인지 선택한다.
- `State Source`: 필요할 때만 플레이어, 소환물 또는 패링 벽의 State를 하나 검사한다.
- `Required Definition`: 필요할 때만 특정 Composable Attack으로 제한한다.
- 나머지 조건이 필요 없으면 기본값을 유지한다.

패링 사건은 Sequence로 구분되므로 연속 패링도 각각 한 번씩 집계된다. 소비 위치는 `BossPatternRunner`가 보관하여 공유되는 StateSO에 런타임 상태가 남지 않는다.
