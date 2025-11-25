using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Action State", menuName = "Scriptable Objects/Dialogue States/Action State", order = 2)]
public class ActionStateSO : DialogueBaseStateSO
{
    private Line currentLine;
    private bool isWaitingGenericInput;
    private bool isWaitingSpecificInput;

    public override void OnEnter(DialogueModule module)
    {
        currentLine = module.CurrentDialogue.lines[module.CurrentLineNum];

        // module.dialogueUI.SetActive(false);
        // module.choicePanel.SetActive(false);

        // Start Action Event
        currentLine.actionEvent?.Invoke();

        var opt = currentLine.actionOptions;

        if (opt.waitSpecificInput && opt.inputKey != EnumManager.InputType.None)
        {
            isWaitingSpecificInput = true;
        }
        else if (opt.waitInputAfterAction)
        {
            isWaitingGenericInput = true;
        }
        else if (opt.autoNextDelay > 0f)
        {
            module.RunCoroutine(WaitAndNext(module, opt.autoNextDelay));
            module.dialogueUI.SetActive(false);
            module.choicePanel.SetActive(false);
        }
        else
        {
            module.CheckDialogueEnd();
        }
    }

    public override void OnUpdate(DialogueModule module)
    {
        if (isWaitingSpecificInput)
        {
            if (GameManager.instance.inputManager.GetInputByEnum(currentLine.actionOptions.inputKey))
            {
                isWaitingSpecificInput = false;
                module.CheckDialogueEnd();
            }

            return;
        }

        // is Waiting SpaceBar Input
        if (isWaitingGenericInput)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                isWaitingGenericInput = false;
                module.CheckDialogueEnd();
            }
        }
    }
    public override void OnExit(DialogueModule module)
    {
        isWaitingSpecificInput = false;
        isWaitingGenericInput = false;
    }

    private IEnumerator WaitAndNext(DialogueModule module, float delay)
    {
        yield return new WaitForSeconds(delay);
        module.CheckDialogueEnd();
    }
}
