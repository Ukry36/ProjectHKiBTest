using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using DG.Tweening;
using TMPro;
using R3;
using UnityEngine.EventSystems;

public class DialogueModule : InterfaceModule, IDialogueable
{
    // === UI ===
    public TextMeshProUGUI lineText;
    public TextMeshProUGUI characterName;
    public TextMeshProUGUI choiceLineLabel;
    public GameObject dialogueUI;
    public GameObject choicePanel;
    public RectTransform nextArrowRect;
    public RectTransform cursorArrow;

    public ChoiceButton[] choiceButtons;
    public ScriptableObject currentStateSO;

    [System.Serializable]
    public struct DialogueVariable // Variable Struct
    {
        public string key;
        public string value;
    }
    public DialogueVariable[] initialVariables;

    //private Dictionary<string, string> variableTable;
    private Dictionary<string, ReactiveProperty<string>> variableTable;

    // === State Machine Parameter ===
    public DialogueDataSO CurrentDialogue { get; set; }
    public int CurrentLineNum { get; set; }
    public void RunCoroutine(IEnumerator routine) { StartCoroutine(routine); }

    // === DOTween ===
    public Tweener LinePrintingTweener { get; set; }
    private Tween nextArrowMoveTween;
    private Vector2 nextArrowBasePos;


    // **State SO Reference**
    public DialogueBaseStateSO dialogueState;
    public DialogueBaseStateSO choiceState;
    public DialogueBaseStateSO actionState;
    public DialogueBaseStateSO endState;
    public StateMachineSO dialogueStateMachine;

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IDialogueable>(this);
    }

    public void Initialize()
    {
        // DOTween Tweener Initialize (for recycle)
        LinePrintingTweener = null;

        if (nextArrowRect != null)
        {
            nextArrowBasePos = nextArrowRect.anchoredPosition;
            nextArrowRect.gameObject.SetActive(false);
        }

        InitializeVariables();
    }

    private Line currentLine;
    private int subLineIndex = 0;
    public void StartDialogue(DialogueDataSO dialogueData)
    {
        GetComponent<StateController>().ResetStateMachine(dialogueStateMachine);
        GameManager.instance.inputManager.MENUMode();
        CurrentDialogue = dialogueData;
        CurrentLineNum = 0;
        dialogueUI.SetActive(true);
    }

    public bool CheckDialogueEnd() => CurrentLineNum < CurrentDialogue.lines.Length;

    public void ExitDialogue()
    {
        GetComponent<StateController>().EliminateStateMachine();
        GameManager.instance.inputManager.PLAYMode();
        dialogueUI.SetActive(false);
        choicePanel.SetActive(false);
    }

    public void HandleInput() //?
    {
        //CurrentState.UpdateState(this);
    }

    public void StartLine()
    {
        currentLine = CurrentDialogue.lines[CurrentLineNum];
        subLineIndex = 0;

        HideNextArrow();

        // {KEY} RESOLVE
        string resolvedName = ResolveVariables(currentLine.characterName);

        // Set Character Name AND Text
        characterName.text = resolvedName;

        PrintCurrentSubLine();
    }

    public void StartChoice()
    {
        choicePanel.SetActive(true);

        // Choice Button Text = Can Write First Line
        if (choiceLineLabel != null)
        {
            if (currentLine.lines != null && currentLine.lines.Length > 0)
                choiceLineLabel.text = ResolveVariables(currentLine.lines[0]);
            else
                choiceLineLabel.text = "";
        }

        // Settin Button
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            var btn = choiceButtons[i];

            if (currentLine.choices != null && i < currentLine.choices.Length)
            {
                btn.gameObject.SetActive(true);

                btn.Setup(
                    ResolveVariables(currentLine.choices[i].choiceText),
                    currentLine.choices[i].nextLineIndex,
                    OnChoiceSelected
                );

                // Choice Arrow
                btn.cursorArrow = cursorArrow;
            }
            else
            {
                btn.gameObject.SetActive(false);
            }
        }

        // CUrsor on First Panel
        if (choiceButtons.Length > 0)
        {
            var first = choiceButtons[0];
            EventSystem.current.SetSelectedGameObject(first.gameObject);
            first.Focus();
        }
    }

    public bool CheckLineType(StateSO type) => type == CurrentDialogue.lines[CurrentLineNum].nextState;

    public bool CheckLineEnd()
    {
        if (LinePrintingTweener != null && LinePrintingTweener.IsActive() && LinePrintingTweener.IsPlaying())
        {
            LinePrintingTweener.Complete();
            return false;
        }

        subLineIndex++;

        if (currentLine.lines != null && subLineIndex < currentLine.lines.Length)
        {
            PrintCurrentSubLine();
            return false;
        }

        return true;
    }

    public void SetLine(int lineNum) => CurrentLineNum = lineNum;
    public void NextLine() => CurrentLineNum++;

    private void PrintCurrentSubLine()
    {
        string raw = currentLine.lines[subLineIndex];
        string resolved = ResolveVariables(raw);

        lineText.text = "";

        // DOTween Sequence Initializzen and ReUse

        if (LinePrintingTweener != null && LinePrintingTweener.IsActive())
        {
            LinePrintingTweener.Kill();
        }
        // Teyping Effect Animation
        LinePrintingTweener = lineText
            .DOText(resolved, currentLine.duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => { ShowNextArrow(); });

        LinePrintingTweener?.Play();
    }

    public void OnChoiceSelected(int logicalIndex)
    {
        if (CurrentDialogue == null || CurrentDialogue.lines == null || CurrentDialogue.lines.Length == 0)
        {
            Debug.LogWarning("OnChoiceSelected: currentDialogue is null or empty");
            //ChangeState(endState);
            return;
        }

        // Change Index > Line Index
        int arrayIndex = CurrentDialogue.FindLineIndexByLogicalIndex(logicalIndex);

        if (arrayIndex < 0)
        {
            Debug.LogWarning($"OnChoiceSelected: logicalIndex {logicalIndex} not found");
            //ChangeState(endState);
            return;
        }

        CurrentLineNum = arrayIndex - 1;

        if (choicePanel) choicePanel.SetActive(false);
        if (dialogueUI) dialogueUI.SetActive(true);

        CheckDialogueEnd();
    }

    public void ShowNextArrow()
    {
        if (nextArrowRect == null) return;

        // Arrow Animation Removed
        if (nextArrowMoveTween != null && nextArrowMoveTween.IsActive())
        {
            nextArrowMoveTween.Kill();
        }

        nextArrowRect.anchoredPosition = nextArrowBasePos;
        nextArrowRect.gameObject.SetActive(true);

        // Shake Animaion
        nextArrowMoveTween = nextArrowRect
            .DOAnchorPosY(nextArrowBasePos.y + 3f, 1.0f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void HideNextArrow()
    {
        if (nextArrowMoveTween != null && nextArrowMoveTween.IsActive())
        {
            nextArrowMoveTween.Kill();
            nextArrowMoveTween = null;
        }

        if (nextArrowRect != null)
        {
            nextArrowRect.anchoredPosition = nextArrowBasePos;
            nextArrowRect.gameObject.SetActive(false);
        }
    }

    private void InitializeVariables()
    {
        variableTable = new Dictionary<string, ReactiveProperty<string>>();

        if (initialVariables == null) return;

        foreach (var v in initialVariables)
        {
            if (string.IsNullOrEmpty(v.key)) continue;

            variableTable[v.key] = new ReactiveProperty<string>(v.value ?? string.Empty);
        }
    }

    // Can Set Variable WHen Outside.
    public void SetVariable(string key, string value)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (variableTable == null)
        {
            variableTable = new Dictionary<string, ReactiveProperty<string>>();
        }

        if (!variableTable.TryGetValue(key, out var rp))
        {
            rp = new ReactiveProperty<string>(value ?? string.Empty);
            variableTable[key] = rp;
        }
        else
        {
            rp.Value = value ?? string.Empty;
        }
    }

    public string GetVariable(string key, string defaultValue = "")
    {
        if (variableTable == null) return defaultValue;

        if (variableTable.TryGetValue(key, out var rp) && rp != null)
        {
            return rp.Value ?? defaultValue;
        }

        return defaultValue;
    }

    // Change {KEY} to Text 
    private static readonly Regex variableRegex = new Regex(@"\{([A-Za-z0-9_]+)\}",
    RegexOptions.Compiled);

    public string ResolveVariables(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (variableTable == null) return input;

        return variableRegex.Replace(input, match =>
        {
            var key = match.Groups[1].Value;

            if (variableTable.TryGetValue(key, out var rp) && rp != null)
            {
                return rp.Value ?? "";
            }

            // If Don't Have
            return match.Value;
        });
    }

    public ReadOnlyReactiveProperty<string> ObserveVariable(string key, string defaultValue = "")
    {
        if (variableTable == null)
        {
            variableTable = new Dictionary<string, ReactiveProperty<string>>();
        }

        if (!variableTable.TryGetValue(key, out var rp) || rp == null)
        {
            // If not Set defaultValue
            rp = new ReactiveProperty<string>(defaultValue);
            variableTable[key] = rp;
        }

        return rp;
    }

}
