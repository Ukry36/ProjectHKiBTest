using System;
using System.Collections;
using System.Linq;
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

    public StateTransition[] transitions;
    public StateActionSO[] enterActions;
    public StateActionSO[] updateActions;
    public StateActionSO[] exitActions;
    public ActionSequence[] actionSequence;
    public bool loopActionSequence;
    public bool useTimer;
    [NaughtyAttributes.ShowIf("useTimer")] [NaughtyAttributes.MinValue(0)][NaughtyAttributes.MaxValue(9)] public int timerID;
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
            if (!stateController.TransitionConditions[i])                     continue;

            if (transitions[i].CheckDecisions(stateController))
            {
                if (transitions[i].action) {transitions[i].action.Act(stateController);}
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
            if (!stateController.TransitionConditions[i])                     continue;
            if (inputType != transitions[i].activationInput)                  continue;

            if (transitions[i].CheckDecisions(stateController))
            {
                if (transitions[i].action) {transitions[i].action.Act(stateController);}
                if (transitions[i].trueState) { stateController.ChangeState(transitions[i].trueState); break; }
            }
            else
            {
                if (transitions[i].falseState) { stateController.ChangeState(transitions[i].falseState); break; }
            }
        }
    }
}
