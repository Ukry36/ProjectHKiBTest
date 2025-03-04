
using DG.Tweening;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    private DialogueDataSO currentDialogue;
    private int currentLineNum;
    public PopUpManager popUpManager;
    private bool isPrintingLine;
    public TextMeshProUGUI lineText;
    public TextMeshProUGUI characterName;
    public GameObject dialogueUI;
    public Sequence linePrintingSequence;

    public void Initialize()
    {

    }

    public void StartDialogue(DialogueDataSO dialogueData)
    {
        currentDialogue = dialogueData;
        currentLineNum = 0;
        dialogueUI.SetActive(true);
    }

    public void OnNextLine() //get input in dialogue mode
    {
        if (isPrintingLine)
        {
            isPrintingLine = false;
            linePrintingSequence.Complete();
        }
        else
        {
            if (++currentLineNum >= currentDialogue.lines.Length)
            {
                OnEndDialogue();
                return;
            }
            characterName.text = currentDialogue.lines[currentLineNum].characterName;
            isPrintingLine = true;
            lineText.text = currentDialogue.lines[currentLineNum].line;
            lineText.maxVisibleCharacters = 0;
            linePrintingSequence.Append(DOTween.To
            (
                x => lineText.maxVisibleCharacters = (int)x,
                0f,
                lineText.text.Length,
                currentDialogue.lines[currentLineNum].duration
            ));
            linePrintingSequence.Play();
        }
    }

    public void OnSkip() //get input in dialogue mode
    {
        popUpManager.Initialize(currentDialogue.summaryTitle, currentDialogue.summary, OnEndDialogue);
        popUpManager.PopUp();
    }

    public void OnEndDialogue()
    {
        dialogueUI.SetActive(false);

    }
}