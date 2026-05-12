public interface IEmotionModule : IInitializable
{
    void ApplyColor(EmotionColor color, int stack, float overrideDuration = -1f);
    int GetStacks(EmotionColor color);
    bool HasColor(EmotionColor color);
    void RemoveColor(EmotionColor color, int stack = 1);
    string GetApproxRomanStack(EmotionColor color);
}