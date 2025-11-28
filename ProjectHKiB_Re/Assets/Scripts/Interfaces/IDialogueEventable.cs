using UnityEditor.Animations;

public interface IDialogueEventable : IInitializable
{
    public DialogueDataSO DialogueData { get; set; }
}