public interface IEmotionModule : IInitializable
{
    void ApplyColor(EmotionColor color, int stack, float overrideDuration = -1f);
    void ApplyColor(EmotionColor color, Gear sourceGear, int stack, float overrideDuration = -1f);

    void ApplyColor(
        EmotionColor color,
        int stack,
        EmotionModule.EmotionApplyTarget applyTarget,
        float overrideDuration = -1f
    );

    void ApplyColor(
        EmotionColor color,
        Gear sourceGear,
        int stack,
        EmotionModule.EmotionApplyTarget applyTarget,
        float overrideDuration = -1f
    );

    int GetStacks(EmotionColor color);
    int GetStacks(EmotionColor color, Gear sourceGear);
    int GetStacks(EmotionColor color, EmotionModule.EmotionApplyTarget applyTarget);
    int GetStacks(EmotionColor color, Gear sourceGear, EmotionModule.EmotionApplyTarget applyTarget);

    bool HasColor(EmotionColor color);
    bool HasColor(EmotionColor color, Gear sourceGear);
    bool HasColor(EmotionColor color, EmotionModule.EmotionApplyTarget applyTarget);
    bool HasColor(EmotionColor color, Gear sourceGear, EmotionModule.EmotionApplyTarget applyTarget);

    void RemoveColor(EmotionColor color, int stack = 1);
    void RemoveColor(EmotionColor color, Gear sourceGear, int stack = 1);
    void RemoveColor(EmotionColor color, EmotionModule.EmotionApplyTarget applyTarget, int stack = 1);
    void RemoveColor(EmotionColor color, Gear sourceGear, EmotionModule.EmotionApplyTarget applyTarget, int stack = 1);

    string GetApproxRomanStack(EmotionColor color);
    string GetApproxRomanStack(EmotionColor color, Gear sourceGear);
    string GetApproxRomanStack(EmotionColor color, EmotionModule.EmotionApplyTarget applyTarget);
    string GetApproxRomanStack(EmotionColor color, Gear sourceGear, EmotionModule.EmotionApplyTarget applyTarget);
}
