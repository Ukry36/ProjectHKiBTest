using UnityEngine;

public class DialogueEventModule : InterfaceModule, IDialogueEventable
{
    [field: SerializeField] public DialogueDataSO DialogueData { get; set; }

    public override void Register(IInterfaceRegistable interfaceRegistable)
    {
        interfaceRegistable.RegisterInterface<IDialogueEventable>(this);
    }

    public void Initialize()
    {

    }

    public void StartDialogue()
    {
        GameManager.instance.UIManager.StartDialogue(DialogueData);
    }
}