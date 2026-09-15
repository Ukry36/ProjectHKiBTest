using UnityEngine;
namespace StateMachine
{
    // 단서를 지급하고, 원하면 그 자리에서 단서 보드를 연다.
    //
    // 맵에 등록된 단서를 "그 맵에서 사건이 나면" 주는 경로는 SetRouteEventFlagAction이 담당한다.
    // 이 액션은 그 규칙 밖에서 이벤트가 직접 단서를 쥐여줄 때 쓴다
    // (RouteProgressState.AcquireClueById — 인터넷 게시글 열람이 쓰는 것과 같은 훅).
    //
    // openCodexImmediately는 기존 직렬화 호환을 위해 이름을 유지한다. 이제는 획득 직후 단서 보드만
    // 열고 단서를 자동 선택하지 않는다. 자동 선택은 NEW를 즉시 '확인 완료'로 지워버리기 때문이다.
    [System.Serializable]
    public class AcquireClueAction : StateAction
    {
        public string clueId;
        public bool openCodexImmediately = true;

        public override void Act(StateController stateController)
        {
            RouteModule route = RouteModule.Instance;
            if (route == null || route.Progress == null)
            {
                Debug.LogError("ERROR: AcquireClueAction - RouteModule을 찾을 수 없습니다.");
                return;
            }

            // Progress getter가 CodexModule을 먼저 직접 연결한 뒤 상태를 반환한다. 이 참조를 받은 뒤
            // 지급해야 이벤트 체인이 UI보다 먼저 실행되어도 NEW 이벤트를 놓치지 않는다.
            RouteProgressState progress = route.Progress;
            CodexModule codex = CodexModule.Instance;

            // 이미 갖고 있으면 false가 온다 — 재실행되는 이벤트에서는 정상이므로 오류로 다루지 않는다.
            bool newlyAcquired = progress.AcquireClueById(clueId);
            bool isNew = codex != null && codex.IsClueNew(clueId);
            Debug.Log($"[ClueNEW][AcquireClueAction] RESULT: clue={clueId}, newlyAcquired={newlyAcquired}, " +
                      $"checkpoint={codex?.HasSavedDreamReadingCheckpoint}, isNew={isNew}, " +
                      $"openBoard={openCodexImmediately}, frame={Time.frameCount}");

            if (!openCodexImmediately) return;

            var panel = Object.FindObjectOfType<RouteFinding.UI.ClueBoardPanel>(true);
            if (panel == null)
            {
                Debug.LogWarning($"[AcquireClueAction] 단서 보드 패널을 찾을 수 없어 '{clueId}' 획득 후 보드를 열지 못했습니다.");
                return;
            }

            if (!newlyAcquired)
                Debug.Log($"[AcquireClueAction] '{clueId}'는 이미 획득한 단서입니다. 단서 보드만 엽니다.");

            Debug.Log($"[ClueNEW][AcquireClueAction] 획득 후 단서 보드 열기: clue={clueId}, newlyAcquired={newlyAcquired}, frame={Time.frameCount}");
            panel.Open();
        }
    }
}
