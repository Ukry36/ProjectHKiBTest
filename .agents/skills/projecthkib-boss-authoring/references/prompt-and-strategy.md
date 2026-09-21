# Prompt and operating strategy

## Recommended prompt

```text
$projecthkib-boss-authoring을 사용해 아래 보스전을 구현해줘.

기획 원본: <문서 또는 파일 경로>
Main StateMachineSO: <에셋 경로 또는 아직 없음>
Boss Prefab: <에셋 경로>
출력 폴더: <Assets 아래 보스 폴더>
작업 범위: <전체 / Phase 이름 / Pattern 목록>

요구사항:
- C#을 새로 작성하지 말고 Boss Authoring, SO, Prefab, Inspector 배선으로 구현.
- 기존 Attack/Prefab/Action/Decision을 우선 재사용.
- Special Action 결과와 Pattern Clear를 분리.
- 성공 결과, 오답/회피/미대응 결과, 금지 행동 처벌을 서로 분리하고 금지 행동을 Special Action 실패나 Pattern 실패로 대체하지 않음.
- 여러 Pattern이 같은 목표를 누적하면 동일한 Special Action ID와 Required Count를 사용하고, 독립 목표만 다른 ID로 분리.
- 최초 연출과 재시도 공격이 다르면 Loop Start Beat로 재시도 시작점을 지정.
- 패턴 실행 차수는 BossPatternCountDecision 또는 Boss Pattern Battle Count를 사용하고, 현재 실행 내부 재시도만 Attempt Count를 사용.
- 조건·페이즈 전이를 기본 완료 전이보다 위에, Phase Restart Fallback을 마지막에 유지.
- 각 Phase 완료 시 Validation을 실행하고 마지막에 Agent Snapshot을 저장.
- 기획서에서 구현 보류로 표시한 항목은 작업 범위에 명시되지 않은 한 생성하지 않음.
- 새 일반 Action/Decision이 꼭 필요하면 구현하지 말고, 부족한 기능·재사용 후보·필요 이유만 보고하고 확인 요청.

완료 보고에는 변경한 에셋, Validation 오류/경고 수, 적용한 가정, 미해결 항목, 최소 플레이테스트 절차를 포함해줘.
```

기획 문서가 충분히 구체적이면 Main·Prefab·출력 폴더와 작업 범위만 채워도 된다. 기존 작업을 이어갈 때는 `Boss Authoring > Save Agent Snapshot...`으로 만든 최신 Snapshot 경로를 한 줄 추가한다.

## Recommended task sizing

- 첫 작업은 Main과 Phase 골격까지만 만들고 구조를 확인한다.
- 이후 한 작업에서 Phase 하나 또는 서로 강하게 연결된 Pattern 2~4개를 완성한다.
- 마지막 작업에서 전역 Phase 분기, 전이 우선순위, 전체 Validation과 플레이테스트를 묶는다.

한 번에 보스 전체를 맡겨도 되지만, 기획이 자주 바뀌거나 Pattern 수가 많으면 Phase 단위가 에셋 충돌과 잘못된 전이 순서를 줄인다. 같은 Unity 프로젝트를 여러 에이전트가 동시에 편집하게 하지 않는다.

## Review gates

다음 경우에만 작업을 멈추고 사용자 결정을 요청한다.

- 필요한 일반 Action/Decision 또는 사건 발행 지점이 존재하지 않는다.
- 두 진입 조건이 동시에 참일 때 어느 분기가 우선인지 기획으로 결정할 수 없다.
- 기존 모듈 통합과 새 모듈 추가 중 선택이 필요하다.
- 씬 또는 Prefab 참조 대상이 여러 개이고 잘못 고르면 플레이 결과가 달라진다.

그 외 이름, 정렬, 기본 지연처럼 되돌리기 쉬운 값은 보수적으로 정하고 완료 보고에 남긴다.

## Acceptance checklist

- Main 최초 경로에서 `ResetBossBattleProgressAction`이 한 번 실행된다.
- 각 Phase Launch에서 `BeginBossPhaseAction`이 `StartSubStateMachineAction`보다 앞선다.
- 모든 Pattern State가 정확한 BossPatternSO를 실행하고 완료 Decision도 같은 에셋을 본다.
- 한 Pattern 안에 같은 Special Action ID가 중복되지 않는다.
- 여러 Pattern이 공유하는 Special Action ID는 동일한 논리 목표이며 Required Count가 모두 같다.
- 서로 독립적인 Special Action은 다른 ID를 사용한다.
- 필수 Special Action 완료 규칙에는 실제 필수 항목이 있다.
- 반복 Pattern의 `Loop Start Beat`가 유효하며 최초 전용 연출을 불필요하게 반복하지 않는다.
- 보스전 전체 Pattern 실행·클리어 횟수와 현재 Pattern 내부 Attempt Count를 혼동하지 않는다.
- 예외 분기와 Phase 종료가 기본 완료보다 먼저 평가된다.
- 즉시 금지 행동 처벌과 전투 종료 조건이 일반 Pattern 진행보다 먼저 평가된다.
- 기획서의 성공 결과, 오답·회피·미대응 결과, 금지 행동이 각각 의도한 Action 또는 분기에 연결된다.
- 구현 보류 항목이 새 에셋이나 배선으로 들어가지 않는다.
- Selector fallback이 마지막이며 막힌 경우 Phase 지역 이력만 초기화한다.
- Pattern Timeline의 공격 슬롯과 Prefab/Definition 참조가 의도한 대상을 가리킨다.
- Boss Authoring Validation의 Error가 0이다. Warning은 의도된 것만 남고 이유가 보고된다.
