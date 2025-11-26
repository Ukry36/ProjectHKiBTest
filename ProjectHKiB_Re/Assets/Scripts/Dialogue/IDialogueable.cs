
public interface IDialogueable : IInitializable
{
    // Used When Dialogue Start From Outside
    void StartDialogue(DialogueDataSO dialogueData);
    public bool CheckLineType(StateSO type);
    public bool CheckLineEnd();
    public void StartLine();
    public void ExitDialogue();

    public void BindUpdateLine();
    public void UnBindUpdateLine();
    public void BindUpdateChoice();
    public void UnBindUpdateChoice();

    public void SetLine(int lineNum);
    public void NextLine();

    // When Call CLICK Button
    void OnChoiceSelected(int nextLineIndex);
}
