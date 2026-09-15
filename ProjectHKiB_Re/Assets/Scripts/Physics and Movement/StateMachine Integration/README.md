# Position Movement StateMachine Actions

공격 실행과 무관한 위치 기반 이동 Action과 공용 `PositionReference`를 관리한다. Self, Player, CurrentTarget, World, SceneAnchor 및 offset 규칙을 이동과 공격에서 함께 사용한다.

`PositionReference.snapFinalPositionToGrid`를 켜면 Source와 offset을 계산한 최종 XY 위치를 가장 가까운 그리드 교점에 맞춘다. `gridCellSize`로 한 칸 크기, `gridOrigin`으로 기준 원점을 설정하며 Z 높이는 보존한다. Scene Transform을 ID로 참조하려면 같은 폴더의 `PositionSceneAnchor`를 사용한다.

## MoveToPositionAction

- `InterpolatedPhysicsStep`: 목적지를 `duration` 뒤에 도착하도록 PhysicsManager에 이동을 요청한다. 위치를 프레임마다 덮어쓰지 않고 실제 `HVelocity`와 Physics 모드의 벽·엔티티 충돌 파이프라인으로 이동한다.
- `InstantTeleport`: `IPhysics.RealTeleport`로 논리 위치와 표시 Body를 같은 프레임에 옮긴다. 보간하지 않는다.
- `Navigation`: `INavigationAgent.SetDestination`에 목적지를 전달한다.

State의 `[SerializeReference, SubclassSelector]` 메뉴에서는 `Movement/Move To Position`으로 표시된다. NaughtyAttributes의 `ShowIf + AllowNesting`을 사용하므로 Inspector에는 선택한 Mode에 필요한 설정만 나타난다.

- `InterpolatedPhysicsStep`: `duration`
- `InstantTeleport`: `stopHorizontalMovementBeforeTeleport`
- `Navigation`: `forceRepath`

Player, CurrentTarget, SceneAnchor는 Action을 실행할 때 실제 Scene 위치를 읽는다. 보간 이동은 `EnterActions` 또는 `ActionSequence`에서 한 번 시작하고, 목적지의 `follow`로 추적 여부를 정한다. Navigation 목적지를 계속 재지정하려면 `UpdateActions`를 사용할 수 있다.

`stopHorizontalMovementBeforeTeleport`를 켜면 순간이동 직전에 걷기 상태와 수평 속도를 정지한다. 점프와 낙하에 해당하는 Z 속도는 기존 `IPhysics.StopMove` 규칙대로 유지한다.

`InterpolatedPhysicsStep`은 DOTween이 진행하므로 `UpdateActions`에 두면 매 프레임 재시작된다. 같은 State asset을 여러 엔티티가 사용해도 실행 상태는 엔티티별로 따로 저장된다. 기존 `speed` 직렬화 값은 `duration`으로 자동 이관된다.

이 이동 요청은 물리 갱신 직전에 목표까지 남은 거리와 시간을 이용해 속도를 계산한다. 따라서 느린 속도도 Grid의 정지 임계값이나 셀 스냅에 잘리지 않으며, 이동 중에는 Physics 모드를 유지한다. Tween 레지스트리가 시작 State 이탈과 컨트롤러 비활성화를 확인해 예약을 취소한다.

## PhysicsMode 변경

`SetPhysicsMovementModeAction`은 `Physics/Set Movement Mode` 메뉴에서 선택한다. Grid, Physics, Static 전환 시 `Mode` 값만 직접 바꾸지 않고 PhysicsManager가 점유 셀과 Grid 상태를 함께 정리한다. `EnterActions`에 두면 한 번 전환하고 이후 자동 모드 판정을 따르며, 상태 동안 특정 모드를 계속 유지하려면 `UpdateActions`에 둔다.

## MoveAlongPositionKeyframesAction

`Movement/Move Along Position Keyframes`를 `EnterActions` 또는 `ActionSequence`에서 한 번 실행한다. `UpdateActions`에 두면 매 프레임 다시 시작하므로 사용하지 않는다.

`MovementPath`는 Action 내부에 저장되는 직렬화 class다. 별도 ScriptableObject를 만들지 않는다. `MovementPathPlayback`이 엔티티별 실행 상태를 보관하므로 여러 엔티티가 같은 State를 공유해도 시작점과 진행도가 섞이지 않는다.

공통 설정:

- `Duration`: **경로 1회**의 총 실행 시간(초).
- `Ease`: 모든 Move 구간의 기본 위치 진행 곡선. Linear는 일정 속도다.
- `Repeat Count`: 총 실행 횟수. 1은 한 번, 2는 두 번, 0은 무한 반복이다.
- `Use Physics`: 켜면 기존 벽·엔티티 충돌과 수직 물리를 사용한다. 끄면 재생 중 커스텀 물리의 중력·외력·벽·엔티티 충돌 처리를 생략한다. 완료·취소 후 일반 물리 처리가 재개되며 수평 보행은 정지한다.
- `Steps`: Move 또는 Teleport와 월드 XY 상대 변위 목록. Z는 경로로 제어하지 않는다.

구간의 `Override Timing`을 켜야 `Speed Ratio`와 개별 `Ease`가 표시된다. 기본 속도 비율은 1이며, 구간 시간은 다음처럼 전체 시간 안에서 배분한다.

```text
구간 가중치 = 이동 거리 / Speed Ratio
구간 시간 = Duration × 구간 가중치 / 모든 Move 구간의 가중치 합
```

같은 거리의 두 구간에 속도 비율 1과 2를 지정하고 Duration을 3초로 설정하면 각각 2초, 1초를 사용한다. Ease는 배분된 시간을 바꾸지 않고 구간 내부의 위치 진행률에 적용된다. Back/Elastic 계열은 예정 경로 밖으로 나갈 수 있으며, 내부 전용/커스텀 Ease 열거 값은 Linear로 처리한다.

Teleport는 시간을 소비하지 않는다. 거리가 0인 Move는 건너뛰며, 시간을 배분할 양수 거리의 Move가 하나도 없으면 오류를 출력하고 실행하지 않는다. 단독 순간이동에는 `MoveToPositionAction`의 InstantTeleport를 사용한다.

예시:

```text
Duration: 4
Ease: Linear
Repeat Count: 2
Use Physics: true
Steps:
  Move      (-20,  0)
  Teleport  (  0, -2)
  Move      ( 20,  0)
  Teleport  (  0, -2)
```

각 수평 이동은 2초, 전체는 8초이며 시작점 대비 최종 예정 위치는 `(0, -8)`이다. 상대 변위는 실제 충돌 위치가 아니라 **앞 구간의 예정 도착점**에 누적하며, 다음 반복도 마지막 예정 위치에서 이어진다. 충돌로 막히면 예정 시각에 도착하지 못할 수 있다. 이 Action은 충돌을 판단하거나 이벤트를 발행하지 않으므로 충돌 처리 측에서 취소 Action을 호출해야 한다.

경로는 PhysicsManager 고정 갱신 안에서 진행한다. 구간 경계를 넘는 남은 시간도 다음 구간에 넘기고, 이동 완료 후 순간이동 순서를 지킨다. 극단적으로 짧은 구간/많은 반복은 한 갱신당 1024개 구간까지만 처리하고 남은 시간을 다음 갱신으로 넘긴다. State 변경(재진입 포함), 컨트롤러/PhysicsModule 비활성화, 다른 위치 이동 예약 시 기존 경로를 취소한다.

기존 `keyframes`/`loop` 직렬화 데이터는 legacy 필드에 유지하며 이전 DOTween 경로로 실행한다. 기존 목록이 있으면 해당 설정을 보여준다. 목록을 비우면 새 인라인 Path 설정이 표시된다. 목적지 추적과 Navigation을 섞은 기존 데이터는 상대 변위로 임의 변환하지 않는다.

## CancelPositionMovementAction

`Movement/Cancel Position Movement`는 현재 StateController의 상대 경로와 위치 Tween 예약을 취소하고 수평 보행/속도를 멈춘다. 뒤에 예약된 순간이동과 반복도 실행되지 않는다. 충돌 종류에 따른 이벤트 처리에서 이 Action을 호출하면 된다. 취소 자체는 Z 속도나 NavigationAgent의 경로를 제거하지 않는다.

현재 이동 대상은 기존과 같은 StateController의 `IPhysics`다. `CombatAttackInstance`의 Transform을 공격 슬롯으로 찾아 움직이는 기능은 이 Action에 포함되지 않는다.

## 임시 Physics Layer 변경

`SetPhysicsLayerOverrideAction`은 `IPhysics.WallLayer`와 `FloorLayer`에 이름 있는 임시 변경을 추가한다. 빠른 이동 중 특정 엔티티 Layer를 통과하려면 State의 `EnterActions`에서 Wall Layer의 `Remove`를 선택하고 제외할 Layer를 지정한 뒤, `ExitActions`의 `ClearPhysicsLayerOverrideAction`에서 같은 slot을 해제한다.

- `Replace`: 현재 마스크 전체를 지정 값으로 교체
- `Add`: 지정 Layer를 현재 마스크에 추가
- `Remove`: 지정 Layer를 현재 마스크에서 제외

서로 다른 slot은 적용 순서대로 합성되므로 여러 기능의 임시 변경을 함께 사용할 수 있다. 같은 slot을 다시 설정하면 기존 값을 교체한다. 모든 임시 변경을 즉시 폐기해야 할 때는 `ClearAllPhysicsLayerOverridesAction`을 사용한다. `PhysicsModule`이 비활성화될 때도 원본 마스크로 자동 복원된다.

벽, 다른 엔티티, 바닥을 포함한 모든 커스텀 물리 충돌을 잠시 끄려면 `DisableAllPhysicsCollisionsAction`을 사용하고, 복구 시점에 `RestoreAllPhysicsCollisionsAction`을 같은 slot으로 실행한다. 비충돌 slot이 하나라도 남아 있는 동안에는 다른 Layer override보다 우선하여 Wall/Floor Layer가 모두 비활성화된다. 따라서 여러 상태나 기능이 각각 비충돌 상태를 요청해도 모든 slot이 복구되기 전에는 충돌이 다시 켜지지 않는다.

동적 엔티티 충돌은 양쪽 엔티티의 Wall Layer가 서로의 Collider GameObject Layer를 허용할 때만 발생한다. 어느 한쪽에서 상대 Layer를 제거하면 Grid 이동, `MoveToward`와 일반 Physics 충돌 모두 상대를 통과한다. Floor Layer 변경은 다음 물리 갱신부터 바닥·천장 탐색에 반영된다.
