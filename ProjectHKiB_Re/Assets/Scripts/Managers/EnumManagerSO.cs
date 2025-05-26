
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
        HasDInput,
        HasLInput,
        HasRInput,
        HasUInput,
    }
}
