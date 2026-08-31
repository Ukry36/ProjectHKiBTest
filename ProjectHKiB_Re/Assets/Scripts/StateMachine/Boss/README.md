# Boss StateMachine Hierarchy

## 구성

보스 GameObject에는 기존 `StateController`, `CombatAttackModule`과 함께 `BossPatternRunner`를 추가한다.

권장 실행 위계는 다음과 같다.

```text
Main StateMachineSO
└─ Phase StateMachineSO (Sub)
   └─ Pattern StateSO
      └─ PlayBossPatternAction(BossPatternSO)
```

## 메인에서 페이즈 실행

1. 메인 State에 `StartSubStateMachineAction`을 배치한다.
2. `Sub State Machine`에 페이즈 StateMachineSO를 지정한다.
3. `Return State`에는 해당 페이즈가 끝난 뒤 메인에서 진입할 State를 지정한다.
4. 페이즈 StateMachine의 종료 State에서 `CompleteSubStateMachineAction`을 실행한다.
5. 중단 결과가 필요하면 `CancelSubStateMachineAction`을 사용한다.
6. 부모 Return State에서는 `SubStateMachineResultDecision`으로 Completed/Cancelled를 구분할 수 있다.

서브 시작과 종료는 Action 실행 즉시 StateMachine을 교체하지 않는다. 현재 State 처리와 충돌하지 않도록 다음 `StateController.Update` 시작 시 실행된다. 부모 State는 서브 실행 중 일시정지되는 것이 아니라 정상적으로 Exit되며, 서브 종료 뒤 지정된 Return State에 새로 Enter한다.

## 패턴 타임라인

`Create > State Machine > Boss > Boss Pattern`에서 패턴을 생성한다.

각 Timeline Entry는 다음 값을 가진다.

- `Label`: 인스펙터 식별용 이름
- `Delay`: 이전 Entry 실행 후 기다릴 상대 시간
- `Actions`: Delay 뒤 같은 프레임에 순서대로 실행할 StateAction
- `Recovery Duration`: 마지막 Entry 실행 후 패턴 완료까지의 후딜레이

패턴 전용 State의 `EnterActions`에 `PlayBossPatternAction`을 넣고, 전이 조건에는 `IsBossPatternCompletedDecision`을 사용한다. State가 패턴 완료 전에 바뀌거나 컨트롤러가 비활성화되면 남은 타임라인은 Cancelled로 종료된다. 이미 시작된 Composable Attack 인스턴스는 자동 취소하지 않으므로 필요한 State의 ExitActions에서 공격 슬롯을 별도로 취소한다.

## 시간 규칙

서브 StateMachine의 전이 시간과 BossPatternSO의 Delay/Recovery는 모두 `StateController.Update`가 전달하는 `Time.deltaTime`을 사용한다. 따라서 `Time.timeScale == 0`이면 함께 멈추며, 한 프레임의 초과 시간은 다음 타임라인 Entry로 넘겨 실행 순서를 유지한다. DOTween과 코루틴 예약은 사용하지 않는다.

## Boss Authoring Window

`Tools > ProjectHKiB > Boss Authoring`에서 전용 제작 창을 연다.

### 1단계 기능

- Main → Phase → Pattern 계층 탐색
- 기존 StateSO Custom Inspector 재사용
- 상대 Delay와 누적 시각을 함께 표시하는 Pattern 타임라인
- Beat 추가, 삭제, 순서 변경, 복제
- Main/Phase/Pattern 연결과 Boss Prefab 필수 모듈 검증
- 빈 패턴, 완료 Decision 누락, 잘못된 Start/Return State, Default 공격 슬롯 경고

계층 탐색은 State의 직접 Enter/Update/Exit/ActionSequence와 Transition Action을 확인한다. `GroupAction` 또는 `SequenceAction` 내부에 중첩된 `StartSubStateMachineAction`과 `PlayBossPatternAction`은 계층 항목으로 탐색하지 않으므로, 계층 핵심 Action은 State에 직접 배치한다.

### 2단계 기능

`Create Phase StateMachine`은 다음 항목을 자동 생성한다.

- Main StateMachine 내부의 `{Phase}_Start`, `{Phase}_Returned` State
- 별도 Phase StateMachineSO
- Phase 내부의 `{Phase}_PatternSelector`, `{Phase}_Finish` State
- Start State의 `StartSubStateMachineAction`
- Finish State의 `CompleteSubStateMachineAction`

`Create Pattern + State`는 별도 BossPatternSO와 Phase 내부 Pattern State를 만들고 `PlayBossPatternAction`, `IsBossPatternCompletedDecision`, Phase Initial State로 돌아가는 완료 전이를 연결한다. Pattern Selector에서 각 Pattern State로 들어가는 선택 전이는 랜덤·순차·조건부 등 보스마다 규칙이 다르므로 자동 생성하지 않는다.

자동 생성 State는 각 StateMachineSO의 서브에셋으로 저장된다. 기존 StateMachineGraph가 있는 에셋은 새 State 노드를 자동 추가하지 않으므로, 그래프를 사용하는 경우 생성 후 그래프 동기화는 별도로 수행한다.
