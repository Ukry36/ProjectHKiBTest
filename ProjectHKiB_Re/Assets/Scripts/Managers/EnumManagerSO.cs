
using UnityEngine;
public class EnumManager
{
    public enum AnimDir
    {
        D, R, L, U
    }

    public enum InputType
    {
        OnMove,
        OnSprint,
        OnAttack,
        OnDodge,
        HasDodge,
        HasDInput,
        HasLInput,
        HasRInput,
        HasUInput,
        OnConfirm,
        None,
        OnSubmit,
        OnSkill,
        OnGraffiti,
        OnGraffitiMoveDown,
        OnGraffitiMoveLeft,
        OnGraffitiMoveRight,
        OnGraffitiMoveUp,
        OnGraffitiAttack,
        OnGraffitiSkill,
        OnGraffitiCancel,
        OnGraffitiReset,
        HasAttack,
        HasSkill
    }

    public enum CompareType
    {
        SameAs,
        BiggerThan,
        BiggerOrSameAs,
        SmallerThan,
        SmallerOrSameAs,
        NotSame
    }
}
