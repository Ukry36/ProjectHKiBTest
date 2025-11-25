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

    public override void OnEnter(DialogueModule module)
    {
        currentLine = module.CurrentDialogue.lines[module.CurrentLineNum];

        // Active UI
        module.dialogueUI.SetActive(false);
        module.choicePanel.SetActive(true);

        // Choice Button Text = Can Write First Line
        if (module.choiceLineLabel != null)
        {
            if (currentLine.lines != null && currentLine.lines.Length > 0)
            {
                string firstLine = module.ResolveVariables(currentLine.lines[0]);
                module.choiceLineLabel.text = firstLine;
            }
            else
            {
                module.choiceLineLabel.text = "";
            }
        }

        // Settin Button
        for (int i = 0; i < module.choiceButtons.Length; i++)
        {
            var btn = module.choiceButtons[i];

            if (currentLine.choices != null && i < currentLine.choices.Length)
            {
                btn.gameObject.SetActive(true);

                btn.Setup(
                    module.ResolveVariables(currentLine.choices[i].choiceText),
                    currentLine.choices[i].nextLineIndex,
                    module.OnChoiceSelected
                );

                // Choice Arrow
                btn.cursorArrow = module.cursorArrow;
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }

        // CUrsor on First Panel
        if (module.choiceButtons.Length > 0)
        {
            var first = module.choiceButtons[0];
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            first.Focus();
        }

        GameManager.instance.inputManager.MENUMode();
    }

    public override void OnUpdate(DialogueModule module)
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

    public override void OnExit(DialogueModule module)
    {
        module.choicePanel.SetActive(false);
        module.dialogueUI.SetActive(true);
    }
}
