using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDialogueable : IInitializable
{
    // Used When Dialogue Start From Outside
    void StartDialogue(DialogueDataSO dialogueData);
    
    //Handling Player Input *Next Text, Click.
    void HandleInput(); 
    
    // When Call CLICK Button
    void OnChoiceSelected(int nextLineIndex);
}
