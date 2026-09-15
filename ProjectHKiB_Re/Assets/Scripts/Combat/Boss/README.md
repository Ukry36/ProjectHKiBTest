# Boss Pattern StateMachine

## 개념 구분

- `Phase`: 여러 Pattern을 묶는 큰 진행 단위다.
- `Pattern`: 공격 타임라인과 자신의 클리어 조건을 가진 실행 단위다.
- `Special Action`: Pattern 도중 플레이어가 성공하거나 실패할 수 있는 대응 행동이다. Pattern 하나에 0개 이상 존재한다.
- `Pattern Cleared`: Special Action 결과와 별개인 패턴 자체의 정상 완료 기록이다.

Special Action의 성공·실패·진행 횟수와 과거 Pattern Cleared 기록은 보스전 전체에 유지된다. Phase 전환이나 Phase 재시작은 현재 Phase의 지역 Pattern 실행 이력만 초기화한다. 새 보스전 시작 State에서 `ResetBossBattleProgressAction`을 한 번 실행해야 전체 기록이 초기화된다.

## 권장 위계

```text
Main StateMachineSO
└─ Phase Launch State
   └─ Phase StateMachineSO (Sub)
      ├─ Pattern Selector State
      ├─ Pattern StateSO × N
      │  └─ PlayBossPatternAction(BossPatternSO)
      └─ Finish State
```

`BeginBossPhaseAction`은 Phase Launch에서 `StartSubStateMachineAction`보다 먼저 둔다. Finish State는 `CompleteSubStateMachineAction`으로 Main의 Return State에 돌아간다.

## PatternSO

`Special Actions` 배열의 각 항목은 다음 값을 가진다.

- `ID`: 특수행동의 논리 ID. 여러 Pattern에서 같은 행동을 누적할 때 같은 ID를 사용
- `Required Count`: 성공에 필요한 누적 횟수
- `Required For Pattern Clear`: 이 특수행동 성공이 Pattern Clear에 필요한지 여부
- `Unanswered Failure Reason ID`: 한 타임라인 사이클 동안 대응하지 않았을 때 기록할 실패 이유. 기본값은 `Ignored`

Pattern Clear 규칙은 다음 세 가지다.

- `Timeline Only`: 타임라인 한 사이클 종료 시 클리어
- `Required Special Actions`: `Required For Pattern Clear` 항목이 모두 성공하면 클리어
- `Timeline And Required Special Actions`: 타임라인 종료와 필수 특수행동 성공을 모두 만족하면 클리어

`Loop Timeline Until Cleared`를 켜면 필수 특수행동이 성공할 때까지 타임라인을 반복한다. 첫 사이클은 항상 0번 Beat부터 실행하고, 이후에는 `Loop Start Beat`부터 반복하므로 최초 등장 연출과 재시도 공격을 한 PatternSO 안에서 분리할 수 있다. 실패는 재시도 중 성공으로 바뀔 수 있지만 성공은 다시 실패로 덮어쓰지 않는다.

같은 논리 Special Action을 여러 Pattern이 공유할 수 있다. 예를 들어 P7과 P8에 `AttackWing`을 같은 `Required Count`로 정의하면 두 Pattern의 성공 횟수가 하나로 누적된다. 같은 Pattern 안의 ID 중복이나 공유 ID의 `Required Count` 불일치는 Authoring Validation 오류다.

## Action과 Decision

올바른 대응을 횟수로 보고할 때 `ReportBossSpecialActionProgressAction`, 여러 프레임 유지되는 조건의 false→true 순간만 기록할 때 `ObserveBossSpecialActionConditionAction`을 사용한다. 즉시 성공·실패를 확정할 때는 `ResolveBossSpecialActionAction`을 사용한다. 이 Action의 `Complete Pattern`은 기본적으로 끄고 Pattern Clear 규칙과 분리한다.

페이즈·패턴 진입 조건에는 다음 Decision을 조합한다.

- `BossSpecialActionResultDecision`: 특정 Special Action의 성공·실패
- `BossSpecialActionFailureReasonDecision`: `WrongEmotion`, `NoParry`, `Ignored` 같은 실패 이유
- `BossSpecialActionProgressDecision`: 누적 횟수
- `BossPatternClearedDecision`: 보스전 전체에서 특정 Pattern을 한 번이라도 클리어했는지
- `BossPatternCountDecision`: 특정 Pattern의 보스전 전체 시작·클리어 횟수
- `IsBossPatternCompletedDecision`: 현재 Pattern 실행이 방금 클리어됐는지
- `BossPhasePatternResultDecision`: 현재 Phase 실행 안의 지역 Pattern 결과

`BranchAction`의 `Boss Pattern Battle Count` Source를 사용하면 같은 Pattern의 실행 차수에 따라 공격 방향이나 연출을 바꿀 수 있다. 내부 타임라인 재시도 횟수는 기존 `BossPatternAttemptCountDecision`을 사용한다.

특수행동 실패는 패턴 실패가 아니다. 패턴 실행 자체를 비정상 종료해야 할 때만 `FailBossPatternAction`과 `IsBossPatternFailedDecision`을 사용한다.

그리드 위치 조건은 보스 전용 폴더가 아니라 일반 `Move/Grid Relation` Decision에 있다. Self, Player, Current Target, Registered Target, Scene Anchor, World 위치 사이의 셀·행·열·Manhattan·Chebyshev 관계를 검사할 수 있다.

## 자동 연결

Boss Authoring 창의 `2. Pattern Design / Inspector`와 `3. Create & Auto Wire`에서 Entry Mode를 설정한다.

- `PhaseStart`: Selector에서 즉시 진입
- `AfterPattern`: 선택한 이전 Pattern의 기본 완료 전이를 현재 Pattern으로 연결. `Timeline Only` Pattern은 타임라인이 한 번 실행된 뒤 완료되므로 기획서의 “자동으로” 진행에 해당
- `Conditional`: 선택한 Source State에서 모든 Entry Conditions가 참일 때 진입

Pattern State가 Source인 Conditional 전이는 기본 Pattern Completed 전이보다 앞에 삽입된다. 따라서 `Pattern3 Cleared + Random 50%`, `SpecialAction5 Failure` 같은 예외 분기가 Selector 복귀보다 먼저 평가된다.

Selector의 마지막에는 `Boss Phase Restart Fallback` 전이가 자동 유지된다. 패턴 클리어 뒤 진입 가능한 Pattern 또는 Phase 종료 전이가 없으면 이 전이가 현재 Phase의 지역 이력을 초기화하고 첫 `PhaseStart` Pattern으로 돌아간다. 수동으로 Selector 전이를 추가했다면 fallback을 항상 마지막 순서에 둔다.

## 예시 구성

Phase A:

- Pattern1: `PhaseStart`, SA1·SA2는 선택 특수행동, `Timeline Only`
- Pattern2: `AfterPattern(Pattern1)`, SA3은 선택, SA4는 `Required For Pattern Clear`, `Required Special Actions + Loop`
- Phase C 즉시 분기: Pattern2에서 `SA3 Success` 전이를 Finish보다 우선 연결
- Phase B 분기: `SA2 Success AND SA4 Success AND Pattern1 Cleared`로 Phase A의 Finish에 진입하고 Main Return에서 Phase B Launch로 연결

Phase B:

- Pattern3: `PhaseStart`, `Timeline Only`
- Pattern4: `Conditional(Pattern3 State)`, `Pattern3 Cleared AND RandomDecision(50%)`
- Pattern5: `Conditional(Pattern4 State)`, `SA5 Failure`
- Pattern5 클리어 뒤 다른 후보가 없으면 자동 fallback으로 Phase B 재시작

Phase D는 각 Pattern State의 완료 시점 전이에 `SA5 Success AND Is Current Boss Pattern Completed`를 조합해 Finish로 보낸 뒤 Main Return에서 Phase D Launch를 선택한다.

조건 전이는 배열 위에서부터 평가된다. 즉시 페이즈 분기, 실패 이유별 분기, 조건부 다음 Pattern, 기본 Pattern Completed, Selector fallback 순서를 권장한다.

## AI 에이전트 제작

저장소의 `.agents/skills/projecthkib-boss-authoring`은 보스전을 C# 추가 없이 SO·Prefab·Inspector 배선으로 제작하는 Codex 작업 규칙과 요청 템플릿을 제공한다. 새 작업에서 `$projecthkib-boss-authoring`을 명시하거나 보스 Phase·Pattern 제작을 요청하면 사용할 수 있다.

`Tools > Boss Authoring` 상단의 `Copy Agent Snapshot`은 현재 Main·Phase·Pattern 구조, 타임라인, 전이 순서와 Validation을 Markdown으로 복사한다. `Save Agent Snapshot...`은 같은 내용을 파일로 저장하며, 여러 Pattern에서 공유하는 논리 Special Action ID와 Required Count도 보스전 전체 목록으로 정리한다. 기존 보스를 이어서 작업할 때는 최신 Snapshot을 기획 문서와 함께 전달하되 실제 Unity 에셋을 최종 기준으로 삼는다.
