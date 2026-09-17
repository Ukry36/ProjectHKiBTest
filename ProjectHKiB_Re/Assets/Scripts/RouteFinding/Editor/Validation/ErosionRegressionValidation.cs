using System;
using UnityEditor;
using UnityEngine;

namespace RouteFinding.Editor.Validation
{
    /// <summary>GUI 없이 C07의 단계·해제·저장 호환성을 확인하는 배치 진입점입니다.</summary>
    public static class ErosionRegressionValidation
    {
        [MenuItem("Tools/RouteFinding/Validation/C07 잠식 상태 회귀 검증")]
        public static void Run()
        {
            int pass = 0;
            void Check(bool condition, string name)
            {
                if (!condition) throw new Exception("[ErosionValidation] FAIL: " + name);
                pass++;
            }

            var state = new DreamErosionState(2);
            Check(state.Level == 0 && state.ConsecutiveFailureCount == 0, "initial state");
            Check(!state.RegisterConnectionFailure() && state.Level == 0, "first failure remains level zero");
            Check(state.RegisterConnectionFailure() && state.Level == 1, "second failure level one");
            state.RegisterConnectionFailure(); state.RegisterConnectionFailure();
            Check(state.Level == 2, "fourth failure level two");
            state.RegisterConnectionFailure(); state.RegisterConnectionFailure();
            Check(state.Level == 3, "sixth failure capped level three");
            Check(state.RegisterConnectionSuccess() && state.Level == 0 && state.ConsecutiveFailureCount == 0, "success clears");
            state.ForceLevel(3);
            Check(state.Level == 3 && state.ConsecutiveFailureCount == 6, "force level keeps next failure monotonic");
            Check(state.RegisterClueAcquired() && state.Level == 0, "clue clears");
            state.AddHallucinationClue("hallucination-a");
            DreamErosionSaveData saved = state.Export();
            var restored = new DreamErosionState(2);
            restored.Import(saved);
            Check(restored.Level == 0 && restored.HasHallucinationClue("hallucination-a"), "export import");
            restored.ForceLevel(3);
            restored.Import(null);
            Check(restored.Level == 0 && restored.HallucinationClueIds.Count == 0, "legacy null save defaults safely");
            restored.Import(new DreamErosionSaveData { version = 0, level = 3, hallucinationClueIds = null });
            Check(restored.Level == 0, "legacy version zero defaults safely");
            Debug.Log($"[ErosionValidation] PASS {pass}/11");
        }
    }
}
