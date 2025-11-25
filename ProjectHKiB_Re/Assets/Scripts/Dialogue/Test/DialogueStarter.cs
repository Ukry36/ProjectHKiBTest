using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DialogueStarter : MonoBehaviour
{
    public DialogueModule dialogueModule;
    public DialogueDataSO testDialogue;
    public DialogueDataSO ActionDialogue;

    private bool isDialogueStarted = false;

    void Start()
    {
        dialogueModule.StartDialogue(testDialogue);
    }

    void Update()
    {
        if (!isDialogueStarted && Input.GetKeyDown(KeyCode.J))
        {
            dialogueModule.StartDialogue(ActionDialogue);
            isDialogueStarted = true;
        }
    }
}
