using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, @PlayerAction.IPLAYActions, PlayerAction.IMENUActions
{
    public @PlayerAction inputs;
    public Vector2 MoveInput { get; private set; }
    public Vector2 LastSetMoveInput { get; set; }
    public bool MoveInputPressed { get; private set; }
    public bool ConfirmInput { get; private set; }

    private void Awake()
    {
        inputs = new @PlayerAction();
        inputs.PLAY.SetCallbacks(this);
        inputs.MENU.SetCallbacks(this);
        PLAYMode();
    }
    
    public bool GetInputByEnum(EnumManager.InputType inputType)
    {
        return inputType switch
        {
            EnumManager.InputType.OnMove => !inputs.PLAY.Move.ReadValue<Vector2>().Equals(Vector2.zero),
            EnumManager.InputType.OnSprint => inputs.PLAY.Sprint.inProgress,
            EnumManager.InputType.HasDodge => inputs.PLAY.Dodge.inProgress,
            EnumManager.InputType.HasDInput => inputs.PLAY.Move.ReadValue<Vector2>().y < 0,
            EnumManager.InputType.HasLInput => inputs.PLAY.Move.ReadValue<Vector2>().x < 0,
            EnumManager.InputType.HasRInput => inputs.PLAY.Move.ReadValue<Vector2>().x > 0,
            EnumManager.InputType.HasUInput => inputs.PLAY.Move.ReadValue<Vector2>().y > 0,
            EnumManager.InputType.HasAttack => inputs.PLAY.Attack.inProgress,
            EnumManager.InputType.HasSkill => inputs.PLAY.Skill.inProgress,
            _ => false,
        };
    }

    public void Bind(EnumManager.InputType inputType, Action<InputAction.CallbackContext> action)
    {
        switch (inputType)
        {
            case EnumManager.InputType.OnAttack:   inputs.PLAY.Attack.performed += action; break;
            case EnumManager.InputType.OnDodge:    inputs.PLAY.Dodge.performed += action; break;
            case EnumManager.InputType.OnConfirm:  inputs.PLAY.Confirm.performed += action; break;
            case EnumManager.InputType.OnSkill:    inputs.PLAY.Skill.performed += action; break;
            case EnumManager.InputType.OnGraffiti: inputs.PLAY.GraffitiSystem.performed += action; break;
            case EnumManager.InputType.OnGraffitiMoveDown: inputs.GRAFFITI.MovePressedD.performed += action; break;
            case EnumManager.InputType.OnGraffitiMoveLeft: inputs.GRAFFITI.MovePressedL.performed += action; break;
            case EnumManager.InputType.OnGraffitiMoveRight:inputs.GRAFFITI.MovePressedR.performed += action; break;
            case EnumManager.InputType.OnGraffitiMoveUp:   inputs.GRAFFITI.MovePressedU.performed += action; break;
            case EnumManager.InputType.OnGraffitiAttack:   inputs.GRAFFITI.Attack.performed += action; break;
            case EnumManager.InputType.OnGraffitiSkill:    inputs.GRAFFITI.Skill.performed += action; break;
            case EnumManager.InputType.OnGraffitiReset:    inputs.GRAFFITI.GraffitiSystem.performed += action; break;
            case EnumManager.InputType.OnGraffitiCancel:   inputs.GRAFFITI.Cancel.performed += action; break;
            default: break;
        };
    }

    public void UnBind(EnumManager.InputType inputType, Action<InputAction.CallbackContext> action)
    {
        switch (inputType)
        {
            case EnumManager.InputType.OnAttack:   inputs.PLAY.Attack.performed -= action; break;
            case EnumManager.InputType.OnDodge:    inputs.PLAY.Dodge.performed -= action; break;
            case EnumManager.InputType.OnConfirm:  inputs.PLAY.Confirm.performed -= action; break;
            case EnumManager.InputType.OnSkill:    inputs.PLAY.Skill.performed -= action; break;
            case EnumManager.InputType.OnGraffiti: inputs.PLAY.GraffitiSystem.performed -= action; break;
            case EnumManager.InputType.OnGraffitiMoveDown: inputs.GRAFFITI.MovePressedD.performed -= action; break;
            case EnumManager.InputType.OnGraffitiMoveLeft: inputs.GRAFFITI.MovePressedL.performed -= action; break;
            case EnumManager.InputType.OnGraffitiMoveRight:inputs.GRAFFITI.MovePressedR.performed -= action; break;
            case EnumManager.InputType.OnGraffitiMoveUp:   inputs.GRAFFITI.MovePressedU.performed -= action; break;
            case EnumManager.InputType.OnGraffitiAttack:   inputs.GRAFFITI.Attack.performed -= action; break;
            case EnumManager.InputType.OnGraffitiSkill:    inputs.GRAFFITI.Skill.performed -= action; break;
            case EnumManager.InputType.OnGraffitiReset:    inputs.GRAFFITI.GraffitiSystem.performed -= action; break;
            case EnumManager.InputType.OnGraffitiCancel:   inputs.GRAFFITI.Cancel.performed -= action; break;
            default: break;
        };
    }

    public void PLAYMode()
    {
        inputs.PLAY.Enable();
        inputs.MENU.Disable();
        inputs.GRAFFITI.Disable();
        Debug.Log("PLAYMode");
    }

    public void MENUMode()
    {
        inputs.PLAY.Disable();
        inputs.MENU.Enable();
        inputs.GRAFFITI.Disable();
        Debug.Log("MENUMode");
    }

    public void GRAFFITIMode()
    {
        inputs.PLAY.Disable();
        inputs.MENU.Disable();
        inputs.GRAFFITI.Enable();
        Debug.Log("GRAFFITIMode");
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
        if (MoveInput.x != 0 || MoveInput.y != 0) LastSetMoveInput = MoveInput;
    }

    public void OnMovePressedD(InputAction.CallbackContext context)
    {
        if (!MoveInputPressed) MoveInputPressed = context.started;
    }

    public void OnMovePressedR(InputAction.CallbackContext context)
    {
        if (!MoveInputPressed) MoveInputPressed = context.started;
    }

    public void OnMovePressedU(InputAction.CallbackContext context)
    {
        if (!MoveInputPressed) MoveInputPressed = context.started;
    }

    public void OnMovePressedL(InputAction.CallbackContext context)
    {
        if (!MoveInputPressed) MoveInputPressed = context.started;
    }

    public void OnSprint(InputAction.CallbackContext context) { }

    public void OnAttack(InputAction.CallbackContext context) { }
    
    public void OnDodge(InputAction.CallbackContext context) { }

    public void OnGraffitiSystem(InputAction.CallbackContext context) { }

    public void OnConfirm(InputAction.CallbackContext context)
    {
        ConfirmInput = context.performed;
    }

    public void OnSkill(InputAction.CallbackContext context) { }

    public Action<InputAction.CallbackContext> onMenu;
    public void OnMenu(InputAction.CallbackContext context) => onMenu?.Invoke(context);

    public void OnNavigate(InputAction.CallbackContext context) { }

    public Action<InputAction.CallbackContext> onSubmit;
    public void OnSubmit(InputAction.CallbackContext context) => onSubmit?.Invoke(context);

    public Action<InputAction.CallbackContext> onMENUCancel;
    public void OnCancel(InputAction.CallbackContext context) => onMENUCancel?.Invoke(context);

    public void OnPoint(InputAction.CallbackContext context) { }

    public void OnClick(InputAction.CallbackContext context) { }

    public void OnScrollWheel(InputAction.CallbackContext context) { }

    public void OnMiddleClick(InputAction.CallbackContext context) { }

    public void OnRightClick(InputAction.CallbackContext context) { }

    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }

    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }
}
