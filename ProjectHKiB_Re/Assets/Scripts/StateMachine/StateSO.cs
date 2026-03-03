using System;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "State", menuName = "State Machine/State")]
public class StateSO : ScriptableObject
{
    [Serializable]
    public struct SubDecision
    {
        public StateDecisionSO decision;
        public StateActionSO action;
        public bool negate;
    }

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

    public SubDecision[] subDecisions;
    public FrameDecision[] frameDecisions;

    public virtual void EnterState(StateController stateController)
    {
        for (int i = 0; i < enterActions.Length; i++)
        {
            enterActions[i].Act(stateController);
        }
        ReserveFrameDecisions(stateController);
        ReserveTransitions(stateController);
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
            if (subDecisions[i].negate ^ subDecisions[i].decision.Decide(stateController))
                subDecisions[i].action.Act(stateController);
        }
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

    public void ExitState(StateController stateController)
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

    public void CheckTransition(StateController stateController)
    {
        Debug.Log($"[StateSO] CheckTransition called on {this.name}, transitions count: {transitions.Length}");
        for (int i = 0; i < transitions.Length; i++)
        {
            bool canTransition = true;
            if (!stateController.TransitionConditions[i])
            {
                Debug.Log($"[StateSO] Transition {i} not available yet (availableTime not met)");
                continue;
            }

            Debug.Log($"[StateSO] Transition {i} checking decisions...");
            for (int j = 0; j < transitions[i].decisions.Length; j++)
            {
                var decision = transitions[i].decisions[j].decision;
                Debug.Log($"[StateSO]   Decision {j} type: {decision.GetType().Name}");
                bool decisionResult = decision.Decide(stateController);
                bool negate = transitions[i].decisions[j].negate;
                bool finalResult = decisionResult ^ negate;
                Debug.Log($"[StateSO]   Decision {j}: result={decisionResult}, negate={negate}, final={finalResult}");
                if (!finalResult)
                {
                    canTransition = false;
                    break;
                }
            }
            if (canTransition)
            {
                if (transitions[i].trueState)
                {
                    Debug.Log($"[StateSO] Transition {i} TRUE -> going to {transitions[i].trueState.name}");
                    stateController.ChangeState(transitions[i].trueState);
                }
                break;
            }
            else if (transitions[i].falseState)
            {
                Debug.Log($"[StateSO] Transition {i} FALSE -> going to {transitions[i].falseState.name}");
                stateController.ChangeState(transitions[i].falseState);
            }
        }
    }
}
