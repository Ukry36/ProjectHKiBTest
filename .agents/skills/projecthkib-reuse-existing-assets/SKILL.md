---
name: projecthkib-reuse-existing-assets
description: Before building any new ProjectHKiB system or feature (visual effect, lighting, UI, input, gameplay mechanic, event action), survey what Unity/URP built-ins and existing project assets already provide and extend them with a thin control layer instead of writing a parallel system. Use at the start of any "새 시스템/기능 개발" request.
---

# ProjectHKiB — 있는 자산 먼저 쓰기

새 시스템을 만들기 전에 **이미 있는 것(Unity·URP 내장 기능, 씬·프리팹에 놓인 오브젝트, 기존 매니저·액션)** 을 먼저 찾고,
그걸 조절하는 코드 몇 줄로 끝낼 수 있는지부터 판단한다. 같은 일을 하는 병렬 시스템을 새로 그리지 않는다.

## 왜

2026-10-02 암전/손전등을 처음엔 화면 위에 덮는 셰이더 필터로 만들었다. 팀원 지적대로 프로젝트에는 이미
URP 2D 라이팅(Global Light2D + Sprite-Lit 재질 + Player에 꺼진 Point Light2D)이 깔려 있었고, 필터 방식은
맵에 둔 Light2D(촛불·창문)와 섞이지 못했다. 결국 전역 조명 밝기와 Player 조명만 조절하는 코드로 다시 만들었다.

## 절차

1. **요청을 엔진 기능 이름으로 바꿔 본다.** 예: 암전·손전등 → 2D Light, 흔들림 → Cinemachine Impulse,
   화면 전환 → ScreenEffectManager, 입력 → Input System 액션 맵, 이벤트 연출 → 이벤트 체인 StateAction.
2. **프로젝트에 이미 깔린 것을 확인한다.**
   - 렌더러·설정: `Assets/Settings/Renderer2D.asset`, `Packages/manifest.json`(URP·2D 패키지 버전)
   - System 씬(`Assets/Scenes/System.unity`)과 Player 하위 오브젝트 — 꺼진 채 놓인 컴포넌트가 단서다
   - 맵 씬·프리팹(`Assets/Prefabs/Mapping Utils/room example.prefab` 등)에 이미 쓰인 예시
   - 기존 매니저(`GameManager` 하위), 이벤트 체인 액션(`Scripts/StateMachine/Actions/**`, `SubclassSelector`로 자동 노출)
   - `grep -rn "<엔진 타입명>" --include=*.cs Assets/Scripts`로 그 기능을 다루는 코드가 있는지
3. **있으면 조절 계층만 쓴다.** 기존 오브젝트의 공개 프로퍼티를 바꾸고, 원래 값을 기억했다가 되돌린다.
   코드로 지정할 공개 API가 없는 직렬화 값(예: Light2D의 적용 정렬 레이어)은 이미 맞게 설정된 오브젝트를
   복제하거나 인스펙터에서 맞춘다.
4. **없을 때만 새로 만든다.** 새로 만들면 왜 기존 것으로 안 되는지 코드 주석에 한 줄 남긴다.
5. **사용 경로는 기존 관례를 따른다.** GameManager 산하 매니저 + 이벤트 체인 액션 + (필요하면) 맵 씬 컴포넌트.
   - 새 매니저는 별도 싱글턴으로 세우지 말고 `GameManager`에 필드를 추가하고 그 오브젝트 아래에 둔다
     (예: `GameManager.darknessManager`). 씬에 안 놓였을 때 자동 생성이 필요하면 GameManager 아래에 만들고 필드에 등록한다.
   - 따로 서야 할 분명한 이유(GameManager보다 먼저 떠야 함 등)가 있을 때만 예외로 하고, 그 이유를 클래스 주석에 적는다.

## 보고할 때

- 무엇을 재사용했고 무엇을 새로 만들었는지 구분해서 적는다.
- 재사용한 자산의 한계(예: 조명을 받지 않는 Unlit 스프라이트는 안 어두워짐)를 함께 적는다.

## 참고 사례

- 암전/손전등: `Scripts/Camera/Effects/DarknessManager.cs` — Global Light2D 밝기 조절 + Player Point Light2D 재사용·복제.
