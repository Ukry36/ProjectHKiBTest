using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DialogueStarter : MonoBehaviour
{
    public UIManager uIManager;
    public DialogueDataSO testDialogue;
    public DialogueDataSO ActionDialogue;

    private bool isDialogueStarted = false;

    void Start()
    {
        uIManager.StartDialogue(testDialogue);
    }

    void Update()
    {
        if (!isDialogueStarted && Input.GetKeyDown(KeyCode.J))
        {
            uIManager.StartDialogue(ActionDialogue);
            isDialogueStarted = true;
        }
    }
}
