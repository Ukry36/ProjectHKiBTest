# Gameplay Event Special Actions

`GameplayEvent`는 패링, 일반 타격, 넉백, 에어본, 상호작용을 `BossPatternRunner`의 SpecialAction으로 연결하는 작은 공통 사건이다. 사건에는 실제 Source·Target과 각 Root Owner, 발생 순간 State, 선택 ID와 Definition이 들어간다.

## 자동으로 발행되는 사건

- `Parry`: `ParryModule`이 피해·넉백 적용 전에 발행한다.
- `Hit`: `DamagableModule`이 패링되지 않은 `CombatAttackInstance` 접촉 뒤 발행한다. `Value`는 실제 피해량이다.
- `Knockback`: 같은 접촉에서 실제 넉백이 시작됐을 때 추가로 발행한다.
- `Interaction`: `InteractionEventTrigger`가 성공했을 때 발행한다. Event ID를 비우면 연결된 `GameEvent.name`을 사용한다.

에어본이나 프로젝트 고유 행동은 해당 State의 Action에 `EmitGameplayEventAction`을 하나 추가한다. 코드 시스템이라면 `GameplayEventDispatcher.Publish`를 호출해 Source와 Target을 둘 다 명시할 수 있다.

## Boss 설정

패턴을 실행하는 State의 `UpdateActions`에 `ReportBossGameplayEventSpecialAction`을 추가한다.

필수 설정은 `Special Action Id`, `Event Type`, `Boss Role` 세 가지다. 흔한 공격은 역할을 Source 또는 Target으로 둔다. 보스와 직접 연결되지 않은 레버 같은 퍼즐 상호작용은 `Boss Role = Any`와 고유한 `Required Event Id`를 함께 사용한다.

더 좁혀야 할 때만 다음 선택 필터를 사용한다.

- `Required Event Id`: 같은 사건 종류 안의 구체 행동 이름
- `Required Definition`: 특정 `CombatAttackDefinitionSO`, `DamageDataSO`, `GameEvent` 등
- `State Source` + `Required State`: 실제 보조 오브젝트 또는 Root Owner의 State 하나
- `Signal Key`: 같은 사건 종류를 읽는 Action이 둘 이상일 때만 서로 다른 값

## 보조 오브젝트

`InstanciateObjectAction`이 만든 `StateController`에는 `EntityOwnershipModule`이 자동 추가된다. 따라서 보스가 소환한 이동 오브젝트의 타격과 플레이어가 만든 패링 벽의 패링 모두 실제 오브젝트 State와 원래 본체 State를 함께 남긴다. Prefab에 별도 소유권 설정은 필요 없다.
