using UnityEngine;

public interface IGraffitiableBase
{
    public StateSO GraffitiAttackState { get; set; }
    public StateSO GraffitiSkillState { get; set; }
    public Vector2 GraffitiTinkerOffset { get; set; }
}

public interface IGraffitiable : IGraffitiableBase, IInitializable
{
    
}