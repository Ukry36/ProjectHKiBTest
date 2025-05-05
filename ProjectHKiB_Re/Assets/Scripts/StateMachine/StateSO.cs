using System;
using System.Collections;
using UnityEditorInternal;
using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "Scriptable Objects/State Machine/State", order = 2)]
public class StateSO : ScriptableObject
{
    [Serializable]
    public struct SubDecision
    {
        public StateDecisionSO decision;
        public StateActionSO action;
    }

    [Serializable]
    public struct FrameDecision
    {
        public float time;
        public StateActionSO action;
    }

    public string animationName;
    public bool directionDependent = true;
    public StateTransition[] transitions;
    public StateActionSO[] enterActions;
    public StateActionSO[] updateActions;
    public StateActionSO[] exitActions;

    public SubDecision[] subDecisions;
    public FrameDecision[] frameDecisions;

    public void EnterState(StateController stateController)
    {
        for (int i = 0; i < enterActions.Length; i++)
        {
            enterActions[i].Act(stateController);
        }
        stateController.PlayStateAnimation(animationName, directionDependent);
        ReserveFrameDecisions(stateController);
        ReserveTransitions(stateController);
    }

    public void ReserveFrameDecisions(StateController stateController)
    {
        for (int i = 0; i < frameDecisions.Length; i++)
            stateController.FrameActionSequences[i] = stateController.StartCoroutine(DelayedActionCoroutine(frameDecisions[i], stateController));
    }

    public void ReserveTransitions(StateController stateController)
    {
        for (int i = 0; i < transitions.Length; i++)
        {
            stateController.TransitionConditions[i] = false;
            stateController.TransitionSequences[i] = stateController.StartCoroutine(TransitionWaitAvailableCoroutine(i, transitions[i], stateController));
        }
    }

    public void UpdateState(StateController stateController)
    {
        //Debug.Log("current anim time: " + stateController.animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
        //Debug.Log("current anim frame: " + stateController.animationController.animator.GetCurrentAnimatorStateInfo(0).length * stateController.animationController.animator.GetCurrentAnimatorStateInfo(0).normalizedTime * 60);
        int i;
        for (i = 0; i < updateActions.Length; i++)
        {
            updateActions[i].Act(stateController);
        }
        for (i = 0; i < subDecisions.Length; i++)
        {
            if (subDecisions[i].decision.Decide(stateController))
                subDecisions[i].action.Act(stateController);
        }
    }

    public IEnumerator DelayedActionCoroutine(FrameDecision frameDecision, StateController stateController)
    {
        yield return new WaitForSeconds(frameDecision.time);
        frameDecision.action.Act(stateController);
    }

    public IEnumerator TransitionWaitAvailableCoroutine(int i, StateTransition transition, StateController stateController)
    {
        yield return new WaitForSeconds(transition.availableTime);
        stateController.TransitionConditions[i] = true;
    }

    public void ExitState(StateController stateController)
    {
        for (int i = 0; i < exitActions.Length; i++)
        {
            exitActions[i].Act(stateController);
        }
        CancelFrameDecisions(stateController);
        ResetTransitionTimer(stateController);
    }

    private void CancelFrameDecisions(StateController stateController)
    {
        for (int i = 0; i < frameDecisions.Length; i++)
            stateController.StopCoroutine(stateController.FrameActionSequences[i]);
    }

    private void ResetTransitionTimer(StateController stateController)
    {
        for (int i = 0; i < transitions.Length; i++)
        {
            stateController.TransitionConditions[i] = false;
            stateController.StopCoroutine(stateController.TransitionSequences[i]);
        }
    }

    public void ResetStateTimer(StateController stateController)
    {
        stateController.lastChangeStateTime = 0;
        CancelFrameDecisions(stateController);
        ReserveFrameDecisions(stateController);
        ResetTransitionTimer(stateController);
        ReserveTransitions(stateController);
    }

    public void CheckTransition(StateController stateController)
    {
        for (int i = 0; i < transitions.Length; i++)
        {
            bool canTransition = true;
            if (!stateController.TransitionConditions[i])
                continue;
            for (int j = 0; j < transitions[i].decisions.Length; j++)
            {
                if (!transitions[i].decisions[j].decision.Decide(stateController) ^ transitions[i].decisions[j].negate)
                {
                    canTransition = false;
                    break;
                }
            }
            if (canTransition)
            {
                /*
                string debugMessage = "";
                for (int j = 0; j < transitions[i].decisions.Length; j++)
                {
                    debugMessage += transitions[i].decisions[j].decision.name
                                + ": "
                                + transitions[i].decisions[j].decision.Decide(stateController) + " ";
                }
                debugMessage += "time: " + (Time.time - stateController.lastChangeStateTime);
                Debug.Log(transitions[i].trueState.name + "\n" + debugMessage);
                */
                if (transitions[i].trueState) stateController.ChangeState(transitions[i].trueState);
                break;
            }
            if (transitions[i].falseState) stateController.ChangeState(transitions[i].falseState);
        }
    }
}
