using System;
using System.Collections;
using StateMachine;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct ActionSequence
{
    public float time;
    public StateActionSO action;
}

[CreateAssetMenu(fileName = "State", menuName = "State Machine/State")]
public class StateSO : ScriptableObject
{

    [NaughtyAttributes.Button]
    public void Save()
    {
        AssetDatabase.SaveAssets();
    }
    [HideInInspector] public float temporaryID;

    [NaughtyAttributes.Button]
    public static void Migrate()
    {
        // 1. 프로젝트 내의 모든 StateSO 에셋을 검색합니다.
        string[] guids = AssetDatabase.FindAssets("t:StateSO");
        int migratedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StateSO stateSO = AssetDatabase.LoadAssetAtPath<StateSO>(path);

            if (stateSO == null) continue;

            // 실행 전 기존 데이터 리스트 초기화 (중복 방지)
            stateSO.EnterActions = new StateAction[10];
            stateSO.UpdateActions = new StateAction[10];
            stateSO.ExitActions = new StateAction[10];

            // 2. Enter / Update / Exit 액션 배열 마이그레이션
            MigrateActionArray(stateSO.enterActions, stateSO.EnterActions);
            MigrateActionArray(stateSO.updateActions, stateSO.UpdateActions);
            MigrateActionArray(stateSO.exitActions, stateSO.ExitActions);

            // 3. Transitions 내부의 액션 마이그레이션
            if (stateSO.transitions != null)
            {
                foreach (var transition in stateSO.transitions)
                {
                    if (transition.action != null)
                    {
                        transition.Action = CreateAndCopyAction(transition.action);
                    }
                }
            }

            // 4. 변경 사항 저장 및 Dirty 마크
            EditorUtility.SetDirty(stateSO);
            migratedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"마이그레이션 완료! 총 {migratedCount}개의 StateSO 변환됨.");
    }

    // SO 배열을 순회하며 일반 클래스 List로 변환하는 함수
    private static void MigrateActionArray(StateActionSO[] oldArray, StateAction[] newList)
    {
        if (oldArray == null) return;

        for (int i = 0; i < oldArray.Length; i++)
        {
            newList[i] = CreateAndCopyAction(oldArray[i]);
        }
    }

    // 핵심: 리플렉션을 통해 구 네임스페이스 SO -> 신 네임스페이스 일반 클래스로 변환 및 데이터 복사
    private static string targetAssembly = "Assembly-CSharp"; // 혹시 Assembly Definition(asmdef)을 쓰신다면 해당 이름으로 변경
    private static StateAction CreateAndCopyAction(StateActionSO oldAction)
    {
        string className = oldAction.GetType().Name;

        // 2. 새로운 네임스페이스("StateMachine") 환경에서의 타입 정보를 찾음
        string fullTypeName = $"StateMachine.{className}, {targetAssembly}";
        Type newType = Type.GetType(fullTypeName);

        if (newType == null)
        {
            Debug.LogError($"[마이그레이션 실패] {fullTypeName} 타입을 찾을 수 없습니다. 클래스 명을 확인해주세요.");
            return null;
        }

        // 3. 새로운 일반 C# 클래스 인스턴스 동적 생성
        StateAction newInstance = (StateAction)Activator.CreateInstance(newType);

        // 4. 기존 SO가 가지고 있던 필드 데이터를 리플렉션으로 새 클래스에 복사
        System.Reflection.FieldInfo[] oldFields = oldAction.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (System.Reflection.FieldInfo oldField in oldFields)
        {
            // 유니티 내부 시스템 필드(m_CachedPtr 등)는 제외
            if (oldField.Name.StartsWith("m_")) continue;

            // 새 클래스에서 동일한 이름의 필드를 찾음
            System.Reflection.FieldInfo newField = newType.GetField(oldField.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (newField != null && newField.FieldType == oldField.FieldType)
            {
                // 데이터 값 그대로 복사 (대사 텍스트, 수치 등 값 타입/스트링 등 호환 가능)
                object value = oldField.GetValue(oldAction);
                newField.SetValue(newInstance, value);
            }
        }

        return newInstance;
    }

    public StateTransition[] transitions;
    public StateActionSO[] enterActions;
    [SerializeReference, SubclassSelector] public StateAction[] EnterActions;
    public StateActionSO[] updateActions;
    [SerializeReference, SubclassSelector] public StateAction[] UpdateActions;
    public StateActionSO[] exitActions;
    [SerializeReference, SubclassSelector] public StateAction[] ExitActions;
    public ActionSequence[] actionSequence;
    public bool loopActionSequence;
    public bool useTimer;
    [NaughtyAttributes.ShowIf("useTimer")][NaughtyAttributes.MinValue(0)][NaughtyAttributes.MaxValue(9)] public int timerID;
    [NaughtyAttributes.ShowIf("useTimer")] public float time;

    public virtual void EnterState(StateController stateController)
    {
        for (int i = 0; i < enterActions.Length; i++)
        {
            enterActions[i].Act(stateController);
        }
        //ReserveFrameDecisions(stateController);
        ReserveTransitions(stateController);
        if (useTimer) stateController.Timers[timerID].StartCooltime(time);
        if (actionSequence.Length > 0) stateController.StartActionSequence(actionSequence, loopActionSequence);
    }

    public void ReserveTransitions(StateController stateController)
    {
        for (int i = 0; i < transitions.Length; i++)
        {
            stateController.TransitionConditions[i] = false;
            stateController.TransitionSequences[i] = stateController.StartCoroutine(TransitionWaitAvailableCoroutine(i, stateController));
            stateController.TransitionSequences[i] = stateController.StartCoroutine(TransitionWaitDisableCoroutine(i, stateController));
        }
    }

    public virtual void UpdateState(StateController stateController)
    {
        for (int i = 0; i < updateActions.Length; i++)
            updateActions[i].Act(stateController);
    }

    public IEnumerator TransitionWaitAvailableCoroutine(int i, StateController stateController)
    {
        if (transitions[i].availableTime > 0)
            yield return new WaitForSeconds(transitions[i].availableTime);
        stateController.TransitionConditions[i] = true;
    }

    public IEnumerator TransitionWaitDisableCoroutine(int i, StateController stateController)
    {
        if (transitions[i].disableTime > 0)
        {
            yield return new WaitForSeconds(transitions[i].disableTime);
            stateController.TransitionConditions[i] = false;
        }
    }

    public virtual void ExitState(StateController stateController)
    {
        if (actionSequence.Length > 0) stateController.StopActionSequence();
        for (int i = 0; i < exitActions.Length; i++)
        {
            exitActions[i].Act(stateController);
        }
        ResetTimers(stateController);
    }

    private void ResetTimers(StateController stateController)
    {
        for (int i = 0; i < transitions.Length; i++)
            if (stateController.TransitionConditions[i])
                stateController.TransitionConditions[i] = false;

        stateController.StopAllCoroutines();
    }

    public void ResetStateTimer(StateController stateController)
    {
        ResetTimers(stateController);
        ReserveTransitions(stateController);
    }

    public void CheckDecision(StateController stateController)
    {
        for (int i = 0; i < transitions.Length; i++)
        {
            if (transitions[i].activationInput != EnumManager.InputType.None) continue;
            if (!stateController.TransitionConditions[i]) continue;

            if (transitions[i].CheckDecisions(stateController))
            {
                if (transitions[i].action) { transitions[i].action.Act(stateController); }
                if (transitions[i].trueState) { stateController.ChangeState(transitions[i].trueState); break; }
            }
            else
            {
                if (transitions[i].falseState) { stateController.ChangeState(transitions[i].falseState); break; }
            }
        }
    }

    public void CheckInputDecision(StateController stateController, EnumManager.InputType inputType)
    {
        for (int i = 0; i < transitions.Length; i++)
        {
            if (transitions[i].activationInput == EnumManager.InputType.None) continue;
            if (!stateController.TransitionConditions[i]) continue;
            if (inputType != transitions[i].activationInput) continue;

            if (transitions[i].CheckDecisions(stateController))
            {
                if (transitions[i].action) { transitions[i].action.Act(stateController); }
                if (transitions[i].trueState) { stateController.ChangeState(transitions[i].trueState); break; }
            }
            else
            {
                if (transitions[i].falseState) { stateController.ChangeState(transitions[i].falseState); break; }
            }
        }
    }
}
