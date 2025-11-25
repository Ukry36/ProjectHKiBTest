using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDialogueable : IInitializable
{
    // Used When Dialogue Start From Outside
    void StartDialogue(DialogueDataSO dialogueData);
    public bool CheckLineType(StateSO type);
    public bool CheckDialogueEnd();
    public bool CheckLineEnd();
    public void StartLine();
    public void ExitDialogue();

    public void StartChoice();

    public void SetLine(int lineNum);
    public void NextLine();

    //Handling Player Input *Next Text, Click.
    void HandleInput();

    // When Call CLICK Button
    void OnChoiceSelected(int nextLineIndex);
}
