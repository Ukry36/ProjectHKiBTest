using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using DG.Tweening;
using TMPro;
using R3;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(StateController))]
public class DialogueModule : InterfaceModule, IDialogueable
{
    private static readonly WaitForSeconds _waitForSeconds0_025 = new(0.025f);

    // === UI ===
    public TextMeshProUGUI lineText;
    public TextMeshProUGUI characterName;
    public GameObject choicePanel;
    public RectTransform nextArrowRect;
    public ButtonEnhancer[] choiceButtons;

    [System.Serializable]
    public struct DialogueVariable { public string key; public string value; }
    public DialogueVariable[] initialVariables;

    //private Dictionary<string, string> variableTable;
    private Dictionary<string, ReactiveProperty<string>> variableTable;

    // === State Machine Parameter ===
    public StateMachineSO dialogueStateMachine;
    [field: SerializeField] public DialogueDataSO CurrentDialogue { get; private set; }
    private Line _currentLine;
    [field: NaughtyAttributes.ReadOnly][field: SerializeField] public int LineIndex { get; private set; }
    [field: NaughtyAttributes.ReadOnly][field: SerializeField] public int SubLineIndex { get; private set; }
    [field: NaughtyAttributes.ReadOnly][field: SerializeField] public bool IsLineEnd { get; private set; }

    public void RunCoroutine(IEnumerator routine) { StartCoroutine(routine); }

    // === DOTween ===
    public Tweener LinePrintingTweener { get; set; }
    private Tween nextArrowMoveTween;
    private Vector2 nextArrowBasePos;

    public Action onExitDialogue;

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

    public Line FindLine(int lineIndex)
    {
        Line line = CurrentDialogue.lines.Find((a) => a.index == lineIndex);
        if (line == null) ExitDialogue();

        return line;
    }

    public void StartDialogue(DialogueDataSO dialogueData)
    {
        GameManager.instance.inputManager.MENUMode();
        CurrentDialogue = dialogueData;
        LineIndex = 0;
        SubLineIndex = 0;
        choicePanel.SetActive(false);
        GetComponent<StateController>().ResetStateMachine(dialogueStateMachine);
    }

    public void ExitDialogue()
    {
        GetComponent<StateController>().EliminateStateMachine();
        choicePanel.SetActive(false);
        onExitDialogue.Invoke();
    }

    public void StartLine()
    {
        IsLineEnd = false;
        _currentLine = FindLine(LineIndex);
        SubLineIndex = 0;

        HideNextArrow();

        // {KEY} RESOLVE
        string resolvedName = ResolveVariables(_currentLine.characterName);

        // Set Character Name AND Text
        characterName.text = resolvedName;

        PrintCurrentSubLine();
    }

    public void BindUpdateLine() => GameManager.instance.inputManager.onSubmit += UpdateLineBinder;
    public void UnBindUpdateLine() => GameManager.instance.inputManager.onSubmit -= UpdateLineBinder;
    private void UpdateLineBinder(InputAction.CallbackContext context)
    {
        if (context.started) UpdateLine();
    }
    private void UpdateLine()
    {
        if (LinePrintingTweener != null && LinePrintingTweener.IsActive() && LinePrintingTweener.IsPlaying())
        {
            LinePrintingTweener.Complete();
            return;
        }
        SubLineIndex++;

        if (_currentLine.lines != null && SubLineIndex < _currentLine.lines.Length)
        {
            PrintCurrentSubLine();
            return;
        }
        ManageDialogueExit();
        NextLine();
        IsLineEnd = true;
    }

    public void BindUpdateChoice() => GameManager.instance.inputManager.onSubmit += UpdateChoiceBinder;
    public void UnBindUpdateChoice() => GameManager.instance.inputManager.onSubmit -= UpdateChoiceBinder;
    private void UpdateChoiceBinder(InputAction.CallbackContext context)
    {
        if (context.started) UpdateChoice();
    }
    private void UpdateChoice()
    {
        if (LinePrintingTweener != null && LinePrintingTweener.IsActive() && LinePrintingTweener.IsPlaying())
        {
            LinePrintingTweener.Complete();
            return;
        }
        SubLineIndex++;

        if (_currentLine.lines == null || SubLineIndex == _currentLine.lines.Length)
            StartCoroutine(Choice());

        if (_currentLine.lines != null && SubLineIndex < _currentLine.lines.Length)
            PrintCurrentSubLine();
    }

    public IEnumerator Choice()
    {
        yield return _waitForSeconds0_025;
        choicePanel.SetActive(true);

        // Settin Button
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            Button button = choiceButtons[i].button;

            if (_currentLine.choices != null && i < _currentLine.choices.Length)
            {
                button.gameObject.SetActive(true);
                button.onClick.RemoveAllListeners();
                int index = _currentLine.choices[i].nextLineIndex;
                button.onClick.AddListener(() => OnChoiceSelected(index));
                choiceButtons[i].TMP.text = _currentLine.choices[i].choiceText;
            }
            else button.gameObject.SetActive(false);
        }

        // Cursor on First Panel
        if (choiceButtons.Length > 0) choiceButtons[0].button.Select();
    }

    public bool CheckLineType(StateSO type)
    {
        Line line = FindLine(LineIndex);
        if (line == null) return false;
        return type == line.lineType;
    }
    public bool CheckLineEnd() => IsLineEnd;
    public void SetLine(int lineNum) => LineIndex = lineNum;
    public void NextLine() => LineIndex++;
    public bool ManageDialogueExit()
    {
        Line line = FindLine(LineIndex);
        if (line != null && line.isEndLine)
        {
            ExitDialogue();
            return true;
        }
        return false;
    }

    private void PrintCurrentSubLine()
    {
        string raw = _currentLine.lines[SubLineIndex];
        string resolved = ResolveVariables(raw);

        lineText.text = "";

        if (LinePrintingTweener != null && LinePrintingTweener.IsActive())
            LinePrintingTweener.Kill();

        // Teyping Effect Animation
        LinePrintingTweener = lineText
            .DOText(resolved, _currentLine.duration)
            .SetEase(Ease.Linear)
            .OnComplete(() => { ShowNextArrow(); });

        LinePrintingTweener?.Play();
    }

    public void OnChoiceSelected(int lineNum)
    {
        if (CurrentDialogue == null || CurrentDialogue.lines == null || CurrentDialogue.lines.Count == 0)
        {
            Debug.LogWarning("OnChoiceSelected: currentDialogue is null or empty");
            ExitDialogue();
            return;
        }

        if (!CurrentDialogue.lines.Exists((a) => a.index == lineNum)) ExitDialogue();

        SetLine(lineNum);
        choicePanel.SetActive(false);
        IsLineEnd = true;
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
