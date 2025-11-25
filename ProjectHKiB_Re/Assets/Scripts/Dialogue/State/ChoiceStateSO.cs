using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

[CreateAssetMenu(fileName = "Choice State", menuName = "Scriptable Objects/Dialogue States/Choice State", order = 1)]
public class ChoiceStateSO : DialogueBaseStateSO
{
    private Line currentLine;

    public override void OnEnter()
    {
        currentLine = dialogueModule.currentDialogue.lines[dialogueModule.currentLineNum];

        // Active UI
        dialogueModule.dialogueUI.SetActive(false);
        dialogueModule.choicePanel.SetActive(true);

        // Choice Button Text = Can Write First Line
        if (dialogueModule.choiceLineLabel != null)
        {
            if (currentLine.lines != null && currentLine.lines.Length > 0)
            {
                string firstLine = dialogueModule.ResolveVariables(currentLine.lines[0]);
                dialogueModule.choiceLineLabel.text = firstLine;
            }
            else
            {
                dialogueModule.choiceLineLabel.text = "";
            }
        }

        // Settin Button
        for (int i = 0; i < dialogueModule.choiceButtons.Length; i++)
        {
            var btn = dialogueModule.choiceButtons[i];

            if (currentLine.choices != null && i < currentLine.choices.Length)
            {
                btn.gameObject.SetActive(true);

                btn.Setup(
                    dialogueModule.ResolveVariables(currentLine.choices[i].choiceText),
                    currentLine.choices[i].nextLineIndex,
                    dialogueModule.OnChoiceSelected
                );

                // Choice Arrow
                btn.cursorArrow = dialogueModule.cursorArrow;
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }

        // CUrsor on First Panel
        if (dialogueModule.choiceButtons.Length > 0)
        {
            var first = dialogueModule.choiceButtons[0];
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            first.Focus();
        }

        GameManager.instance.inputManager.MENUMode();
    }

    public override void OnUpdate()
    {
        var input = GameManager.instance.inputManager;

        // Enter -> Confirm INPUT
        if (input.ConfirmInput)
        {
            var obj = EventSystem.current.currentSelectedGameObject;
            if (obj != null)
            {
                var btn = obj.GetComponent<Button>();
                btn?.onClick.Invoke();
            }
        }
    }

    public override void OnExit()
    {
        dialogueModule.choicePanel.SetActive(false);
        dialogueModule.dialogueUI.SetActive(true);
    }
}
