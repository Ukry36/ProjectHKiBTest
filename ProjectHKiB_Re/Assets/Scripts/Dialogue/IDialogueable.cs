
public interface IDialogueable : IInitializable
{
    // Used When Dialogue Start From Outside
    void StartDialogue();
    public void StartLine(Line line);
    public void ExitDialogue();

    public void BindUpdateLine();
    public void UnBindUpdateLine();
    public void BindUpdateChoice();
    public void UnBindUpdateChoice();

    public void NextSubLine();
    public bool IsLineEnded { get; }
    public int ChoicedNum { get; }
    public System.Action onExitDialogue { get; set; }
}
