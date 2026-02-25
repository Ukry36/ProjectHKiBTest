using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "State Machine/State")]
public class StateSO : ScriptableObject
{
    [HideInInspector] public float temporaryID;

    [Serializable]
    public struct FrameDecision
    {
        public float time;
        public StateActionSO action;
    }

    public StateTransition[] transitions;
    public StateActionSO[] enterActions;
    public StateActionSO[] updateActions;
    public StateActionSO[] exitActions;
    public FrameDecision[] frameDecisions;

    public virtual void EnterState(StateController stateController)
    {
        for (int i = 0; i < enterActions.Length; i++)
        {
            enterActions[i].Act(stateController);
        }
        ReserveFrameDecisions(stateController);
        ReserveTransitions(stateController);
        Debug.Log(name);
    }

    public void ReserveFrameDecisions(StateController stateController)
    {
        for (int i = 0; i < frameDecisions.Length; i++)
            stateController.FrameActionSequences[i] = stateController.StartCoroutine(DelayedActionCoroutine(i, stateController));
    }

    public void ReserveTransitions(StateController stateController)
    {
        for (int i = 0; i < transitions.Length; i++)
        {
            stateController.TransitionConditions[i] = false;
            stateController.TransitionSequences[i] = stateController.StartCoroutine(TransitionWaitAvailableCoroutine(i, stateController));
        }
    }

    public virtual void UpdateState(StateController stateController)
    {
        for (int i = 0; i < updateActions.Length; i++)
            updateActions[i].Act(stateController);
    }

    public IEnumerator DelayedActionCoroutine(int i, StateController stateController)
    {
        if (!frameDecisions[i].action) yield break;
        if (frameDecisions[i].time > 0)
            yield return new WaitForSeconds(frameDecisions[i].time);
        frameDecisions[i].action.Act(stateController);
    }

    public IEnumerator TransitionWaitAvailableCoroutine(int i, StateController stateController)
    {
        if (transitions[i].availableTime > 0)
            yield return new WaitForSeconds(transitions[i].availableTime);
        stateController.TransitionConditions[i] = true;
    }

    public virtual void ExitState(StateController stateController)
    {
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
        ReserveFrameDecisions(stateController);
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
