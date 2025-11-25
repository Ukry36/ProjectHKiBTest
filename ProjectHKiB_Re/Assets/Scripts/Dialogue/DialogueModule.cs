using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using R3;

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
    public IState currentState { get; private set; }
    public DialogueDataSO currentDialogue { get; set; }
    public int currentLineNum { get; set; }
    public void RunCoroutine(IEnumerator routine) { StartCoroutine(routine); }
    
    // === DOTween ===
    public Tweener linePrintingTweener { get; set; }
    private Tween nextArrowMoveTween;
    private Vector2 nextArrowBasePos;

     
    // **State SO Reference**
    public DialogueBaseStateSO dialogueState;
    public DialogueBaseStateSO choiceState;
    public DialogueBaseStateSO actionState;
    public DialogueBaseStateSO endState;
    
    // === Initialize & InterFace Resiter
    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IDialogueable>(this);
    }
    public void Initialize()
    {

    }
    
    private void Awake()
    {
        // State SO connecet DialogueModule & Initialize
        dialogueState.Initialize(this);
        choiceState.Initialize(this);
        actionState.Initialize(this);
        endState.Initialize(this);

        // DOTween Tweener Initialize (for recycle)
        linePrintingTweener = null;

        if (nextArrowRect != null)
        {
            nextArrowBasePos = nextArrowRect.anchoredPosition;
            nextArrowRect.gameObject.SetActive(false);
        }
        
        InitializeVariables();
    }    
    
    private void Update()
    {
        // Currrent State OnUpdate Mathod Call Each Frame.
        currentState?.OnUpdate();
    }
    
    // === Core Mathod ===
    public void ChangeState(IState newState)
    {
        currentState?.OnExit();
        currentState = newState;
        if (newState is ScriptableObject so)
            currentStateSO = so;
        else
            currentStateSO = null;

        currentState.OnEnter();
    }

    public void StartDialogue(DialogueDataSO dialogueData)
    {
        currentDialogue = dialogueData;
        currentLineNum = -1;
        dialogueUI.SetActive(true);
        HandleNextLine();
    }

    public void HandleInput()
    {
        currentState?.OnUpdate(); 
    }
    
    public void HandleNextLine()
    {
        currentLineNum++;
        
        // Check End Condition on Dialogue
        if (currentLineNum >= currentDialogue.lines.Length)
        {
            ChangeState(endState);
            return;
        }

        Line nextLine = currentDialogue.lines[currentLineNum];
        
        // Change State Linked Line Date
        ChangeState(nextLine.nextState);
    }
    
    public void OnChoiceSelected(int logicalIndex)
    {
        if (currentDialogue == null || currentDialogue.lines == null || currentDialogue.lines.Length == 0)
        {
            Debug.LogWarning("OnChoiceSelected: currentDialogue is null or empty");
            ChangeState(endState);
            return;
        }

        // Change Index > Line Index
        int arrayIndex = currentDialogue.FindLineIndexByLogicalIndex(logicalIndex);

        if (arrayIndex < 0)
        {
            Debug.LogWarning($"OnChoiceSelected: logicalIndex {logicalIndex} not found");
            ChangeState(endState);
            return;
        }

        currentLineNum = arrayIndex - 1;

        if (choicePanel) choicePanel.SetActive(false);
        if (dialogueUI)  dialogueUI.SetActive(true);

        HandleNextLine();
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
