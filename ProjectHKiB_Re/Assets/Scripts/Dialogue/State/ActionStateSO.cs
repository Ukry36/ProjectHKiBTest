using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Action State", menuName = "Scriptable Objects/Dialogue States/Action State", order = 2)]
public class ActionStateSO : DialogueBaseStateSO
{
    private Line currentLine;
    private bool isWaitingGenericInput;
    private bool isWaitingSpecificInput;

    public override void OnEnter()
    {
        currentLine = dialogueModule.currentDialogue.lines[dialogueModule.currentLineNum];

        // dialogueModule.dialogueUI.SetActive(false);
        // dialogueModule.choicePanel.SetActive(false);

       // Start Action Event
        currentLine.actionEvent?.Invoke();

        var opt = currentLine.actionOptions;

        if (opt.waitSpecificInput && opt.inputKey != KeyCode.None)
        {
            isWaitingSpecificInput = true;
        }
        else if (opt.waitInputAfterAction)
        {
            isWaitingGenericInput = true;
        }
        else if (opt.autoNextDelay > 0f)
        {
            dialogueModule.RunCoroutine(WaitAndNext(opt.autoNextDelay));
            dialogueModule.dialogueUI.SetActive(false);
            dialogueModule.choicePanel.SetActive(false);
        }
        else
        {
            dialogueModule.HandleNextLine();
        }
    }

    public override void OnUpdate()
    {
        if (isWaitingSpecificInput)
        {
            if (Input.GetKeyDown(currentLine.actionOptions.inputKey))
            {
                isWaitingSpecificInput = false;
                dialogueModule.HandleNextLine();
            }

            return;
        }

        // is Waiting SpaceBar Input
        if (isWaitingGenericInput)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                isWaitingGenericInput = false;
                dialogueModule.HandleNextLine();
            }
        }
    }
    public override void OnExit()
    {
        isWaitingSpecificInput = false;
        isWaitingGenericInput = false;
    }

    private IEnumerator WaitAndNext(float delay)
    {
        yield return new WaitForSeconds(delay);
        dialogueModule.HandleNextLine();
    }
}
