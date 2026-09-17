# C05 획득 알림 · C07 꿈 잠식 인계

## 구현 범위

- `ClueAcquireNotice`는 `RouteProgressState.OnClueAcquired(ClueData)`만 구독한다. 따라서 세이브 로드의 직접 컬렉션 복원과 중복 획득은 토스트/효과음을 만들지 않는다.
- 토스트는 대표 아이콘 주소를 `ClueAttachmentService.LoadSprite`로 읽고, 없거나 실패하면 아이콘 오브젝트를 숨긴다. 큐 길이·표시 시간·페이드는 인스펙터 값이며, 효과음 `AudioDataSO` 슬롯은 의도적으로 비워 뒀다.
- 씬에서는 `Tools > RouteFinding > 단서 획득 알림 씬 세팅`을 실행한다. 기존 보드 프리팹을 수정하지 않고 현재 루트 Canvas에 독립 오브젝트를 만든다.
- `DreamErosionState`는 실패 연속수/0~3 단계/환각 단서 ID/버전 DTO를 보유한다. `Import`는 이벤트를 내지 않는다.
- `DreamErosionModule`은 동일 획득 훅으로 잠식을 해제한다. 3단계는 `OnDreamExileRequested`, `OnRandomCluePairSpawnRequested`, `OnHallucinationScatterRequested`만 발행한다. 월드 이동·배치는 하지 않는다.

## Claude에 연결 요청

보드의 **Unrelated 거절이 실제로 판정된 경우에만** 다음을 호출한다. 자기 자신/실루엣/빈 곳/AlreadyConnected 재시도에는 호출하지 않는다.

```csharp
DreamErosionModule.Instance?.RegisterConnectionFailure(boardId, firstNodeId, secondNodeId);
```

새 연결이 실제 성립했을 때만 다음을 호출한다.

```csharp
DreamErosionModule.Instance?.RegisterConnectionSuccess(boardId, relationId);
```

`ClueBoardPanel.ApplySources`의 hallucination provider에는 다음을 전달한다.

```csharp
board => DreamErosionModule.Instance?.GetHallucinationNodeIds(board)
          ?? System.Linq.Enumerable.Empty<string>()
```

현재 `GetHallucinationNodeIds`는 해당 보드 슬롯 중 `slot.clueId`가 영속 환각 ID와 같은 `nodeId`만 반환한다.

## 결정 필요

- 환각 노드를 환각 전용 슬롯에 놓을지, 미획득 슬롯을 랜덤 선점할지.
- 3단계 강제추방의 꿈 목적지, 랜덤 단서 쌍 방의 배치, 전역 환각 단서 산포의 월드/맵 계약.
- 단계당 실패 횟수의 게임 기본값. 현재 기본값은 즉시 단계 상승을 확인하기 쉬운 `1`이며 인스펙터에서 조절한다.
- 붉은 색감·비네트·블러, 초침/BGM 속도는 기존 화면/오디오 API에 영속 조절 계약이 없어 새 콘텐츠 및 월드 연출 명세가 필요하다. 현재 3단계 노이즈와 단계별 `EffectAudioCue` 슬롯만 호출한다.

## Claude 재검증 (2026-09-15)

- 보드 연동 3곳 연결 완료(`ClueBoardScreen.HandleConnectionResult`/`HandleConnectionEstablished`, `ClueBoardPanel.ApplySources`). 보드 쪽 검증 `Logs/ChainRevealLocalListValidation.log`의 잠식 훅 항목 참조.
- Codex 배치 검증을 프로젝트 사본(Assets/Packages/ProjectSettings/Library 복사)에서 다시 실행: `ErosionRegressionValidation` **11/11 PASS**, `AcquireNoticeValidation` **4/4 PASS** (`Logs/C05C07_CodexValidation_rerun.log`). 두 진입점은 `EditorApplication.Exit`를 부르지 않으므로 `-quit`와 함께 실행해야 한다.
- `AcquireNoticeValidation`의 "큐 길이 2" 기대는 잘못이었다 — `StartCoroutine`이 첫 yield까지 즉시 실행돼 첫 단서는 바로 표시되고 둘째만 큐에 남는다(PendingCount 1). 기대값을 고치고 "첫 단서가 먼저 표시" 확인을 추가했다. 컴포넌트 동작은 그대로다.
