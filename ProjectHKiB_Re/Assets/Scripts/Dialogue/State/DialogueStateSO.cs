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

    public override void OnEnter(DialogueModule module)
    {

        GameManager.instance.inputManager.MENUMode();
        currentLine = module.CurrentDialogue.lines[module.CurrentLineNum];
        subLineIndex = 0;

        // UI Active
        module.dialogueUI.SetActive(true);
        module.choicePanel.SetActive(false);
        module.HideNextArrow();

        // {KEY} RESOLVE
        string resolvedName = module.ResolveVariables(currentLine.characterName);

        // Set Character Name AND Text
        module.characterName.text = resolvedName;

        PrintCurrentSubLine(module);
    }

    private void PrintCurrentSubLine(DialogueModule module)
    {
        string raw = currentLine.lines[subLineIndex];
        string resolved = module.ResolveVariables(raw);

        module.lineText.text = "";

        // DOTween Sequence Initializzen and ReUse

        if (module.LinePrintingTweener != null && module.LinePrintingTweener.IsActive())
        {
            module.LinePrintingTweener.Kill();
        }
        // Teyping Effect Animation
        module.LinePrintingTweener = module.lineText
            .DOText(resolved, currentLine.duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => { module.ShowNextArrow(); });

        module.LinePrintingTweener?.Play();
    }


    public override void OnUpdate(DialogueModule module)
    {
        // Player Input(Click) Handling
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            // 타이핑 중이면 스킵
            if (module.LinePrintingTweener != null &&
                module.LinePrintingTweener.IsActive() &&
                module.LinePrintingTweener.IsPlaying())
            {
                module.LinePrintingTweener.Complete();
                return;
            }

            subLineIndex++;

            if (currentLine.lines != null && subLineIndex < currentLine.lines.Length)
            {
                module.HideNextArrow();
                PrintCurrentSubLine(module);
            }
            else
            {
                module.HideNextArrow();
                module.CheckDialogueEnd();
            }
        }
    }

    public override void OnExit(DialogueModule module) { }
}