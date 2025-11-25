using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using DG.Tweening.Core;



[CreateAssetMenu(fileName = "Dialogue State", menuName = "Scriptable Objects/Dialogue States/Dialogue State", order = 0)]
public class DialogueStateSO : DialogueBaseStateSO
{
    private Line currentLine;
    private int subLineIndex = 0;

    public override void OnEnter()
    {

        GameManager.instance.inputManager.MENUMode();
        currentLine = dialogueModule.currentDialogue.lines[dialogueModule.currentLineNum];
        subLineIndex = 0;

        // UI Active
        dialogueModule.dialogueUI.SetActive(true);
        dialogueModule.choicePanel.SetActive(false);
        dialogueModule.HideNextArrow();

        // {KEY} RESOLVE
        string resolvedName = dialogueModule.ResolveVariables(currentLine.characterName);

        // Set Character Name AND Text
        dialogueModule.characterName.text = resolvedName;

        PrintCurrentSubLine();
    }

    private void PrintCurrentSubLine()
    {
        string raw = currentLine.lines[subLineIndex];
        string resolved = dialogueModule.ResolveVariables(raw);

        dialogueModule.lineText.text = "";

        // DOTween Sequence Initializzen and ReUse

        if (dialogueModule.linePrintingTweener != null && dialogueModule.linePrintingTweener.IsActive())
        {
            dialogueModule.linePrintingTweener.Kill();
        }
        // Teyping Effect Animation
        dialogueModule.linePrintingTweener = dialogueModule.lineText
            .DOText(resolved, currentLine.duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => { dialogueModule.ShowNextArrow(); });

        dialogueModule.linePrintingTweener?.Play();
    }

    
    public override void OnUpdate()
    {
        // Player Input(Click) Handling
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            // 타이핑 중이면 스킵
            if (dialogueModule.linePrintingTweener != null &&
                dialogueModule.linePrintingTweener.IsActive() &&
                dialogueModule.linePrintingTweener.IsPlaying())
            {
                dialogueModule.linePrintingTweener.Complete();
                return;
            }

            subLineIndex++;

            if (currentLine.lines != null && subLineIndex < currentLine.lines.Length)
            {
                dialogueModule.HideNextArrow();
                PrintCurrentSubLine();
            }
            else
            {
                dialogueModule.HideNextArrow();
                dialogueModule.HandleNextLine();
            }
        }
    }
}