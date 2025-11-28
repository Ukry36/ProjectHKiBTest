using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, @PlayerAction.IPLAYActions, PlayerAction.IMENUActions
{
    public @PlayerAction inputs;
    public Vector2 MoveInput { get; private set; }
    public Vector2 LastSetMoveInput { get; set; }
    public bool MoveInputPressed { get; private set; }
    public bool DInput { get; private set; }
    public bool RInput { get; private set; }
    public bool UInput { get; private set; }
    public bool LInput { get; private set; }
    public bool AttackInput { get; private set; }
    public bool ChargeInput { get; private set; }
    public bool DodgeInput { get; private set; }
    public bool DodgeProgressInput { get; private set; }
    public bool GraffitiStartInput { get; private set; }
    public bool GraffitiEndInput { get; private set; }
    public bool SkillInput { get; private set; }

    public bool ConfirmInput { get; private set; }
    public bool PauseInput { get; private set; }
    public bool EquipmentOpenCloseInput { get; private set; }
    public bool InventoryOpenCloseInput { get; private set; }

    public bool NextInput { get; private set; }
    public bool PrevInput { get; private set; }
    public bool CancelInput { get; private set; }
    public bool SprintInput { get; private set; }
    /*
        private PlayerInput _playerInput;
        private InputAction move, movePressedD, movePressedR, movePressedU, movePressedL,
                sprint, attack, dodge, grafitti, skill, confirm, cancel, equipment, inventory;

        public bool stopPlayerMovement;
        public bool stopPlayer;
        public bool stopUI;
    */

    private void Awake()
    {/*
        _playerInput = GetComponent<PlayerInput>();

        move = _playerInput.actions["Move"];
        movePressedD = _playerInput.actions["MovePressedD"];
        movePressedR = _playerInput.actions["MovePressedR"];
        movePressedU = _playerInput.actions["MovePressedU"];
        movePressedL = _playerInput.actions["MovePressedL"];
        sprint = _playerInput.actions["Sprint"];
        attack = _playerInput.actions["Attack"];
        dodge = _playerInput.actions["Dodge"];
        grafitti = _playerInput.actions["GraffitiSystem"];
        skill = _playerInput.actions["Skill"];

        confirm = _playerInput.actions["Confirm"];
        cancel = _playerInput.actions["Cancel"];
        equipment = _playerInput.actions["OpenEquipment"];
        inventory = _playerInput.actions["OpenInventory"];
*/
        inputs = new @PlayerAction();
        inputs.PLAY.SetCallbacks(this);
        inputs.MENU.SetCallbacks(this);
        PLAYMode();
    }
    /*
        private void Update()
        {
            MoveInput = move.ReadValue<Vector2>();
            if (MoveInput.x != 0 || MoveInput.y != 0)
                LastSetMoveInput = MoveInput;
            MoveInputPressed = movePressedD.WasPressedThisFrame() || movePressedR.WasPressedThisFrame() || movePressedU.WasPressedThisFrame() || movePressedL.WasPressedThisFrame();
            DInput = movePressedD.WasPressedThisFrame();
            RInput = movePressedR.WasPressedThisFrame();
            UInput = movePressedU.WasPressedThisFrame();
            LInput = movePressedL.WasPressedThisFrame();
            AttackInput = attack.WasPressedThisFrame();
            ChargeInput = attack.inProgress;
            DodgeInput = dodge.WasPressedThisFrame();
            DodgeProgressInput = dodge.inProgress;
            GraffitiStartInput = grafitti.WasPressedThisFrame();
            GraffitiEndInput = grafitti.WasReleasedThisFrame();
            SkillInput = skill.WasPressedThisFrame();
            ConfirmInput = confirm.WasPressedThisFrame();

            PauseInput = cancel.WasPressedThisFrame();


            UIConfirmInput = confirm.WasPressedThisFrame();
            EquipmentOpenCloseInput = equipment.WasPressedThisFrame();
            InventoryOpenCloseInput = inventory.WasPressedThisFrame();


            SprintInput = sprint.inProgress;
            CancelInput = cancel.WasPressedThisFrame();
            NextInput = equipment.WasPressedThisFrame();
            PrevInput = inventory.WasPressedThisFrame();
        }
    */
    public bool GetInputByEnum(EnumManager.InputType inputType)
    {
        return inputType switch
        {
            EnumManager.InputType.OnMove => !MoveInput.Equals(Vector2.zero),
            EnumManager.InputType.OnSprint => SprintInput,
            EnumManager.InputType.OnAttack => AttackInput,
            EnumManager.InputType.OnDodge => DodgeInput,
            EnumManager.InputType.HasDodge => DodgeProgressInput,
            EnumManager.InputType.HasDInput => MoveInput.y < 0,
            EnumManager.InputType.HasLInput => MoveInput.x < 0,
            EnumManager.InputType.HasRInput => MoveInput.x > 0,
            EnumManager.InputType.HasUInput => MoveInput.y > 0,
            EnumManager.InputType.OnConfirm => ConfirmInput,
            _ => false,
        };
    }

    public void PLAYMode()
    {
        inputs.PLAY.Enable();
        inputs.MENU.Disable();
        Debug.Log("PLAYMode");
    }

    public void MENUMode()
    {
        inputs.PLAY.Disable();
        inputs.MENU.Enable();
        Debug.Log("MENUMode");
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
        if (MoveInput.x != 0 || MoveInput.y != 0)
            LastSetMoveInput = MoveInput;
    }

    public void OnMovePressedD(InputAction.CallbackContext context)
    {
        DInput = context.started;
        if (!MoveInputPressed) MoveInputPressed = context.started;
    }

    public void OnMovePressedR(InputAction.CallbackContext context)
    {
        RInput = context.started;
        if (!MoveInputPressed) MoveInputPressed = context.started;
    }

    public void OnMovePressedU(InputAction.CallbackContext context)
    {
        UInput = context.started;
        if (!MoveInputPressed) MoveInputPressed = context.started;
    }

    public void OnMovePressedL(InputAction.CallbackContext context)
    {
        LInput = context.started;
        if (!MoveInputPressed) MoveInputPressed = context.started;
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        SprintInput = context.performed;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        AttackInput = context.started;
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        DodgeInput = context.started;
        DodgeProgressInput = context.performed;
    }

    public void OnGraffitiSystem(InputAction.CallbackContext context)
    {
        GraffitiStartInput = context.started;
        GraffitiEndInput = context.canceled;
    }

    public void OnConfirm(InputAction.CallbackContext context)
    {
        ConfirmInput = context.performed;
    }

    public void OnSkill(InputAction.CallbackContext context)
    {
        SkillInput = context.started;
    }

    public Action<InputAction.CallbackContext> onMenu;
    public void OnMenu(InputAction.CallbackContext context)
    {
        CancelInput = context.started;
        onMenu?.Invoke(context);
    }

    public void OnNavigate(InputAction.CallbackContext context) { }

    public Action<InputAction.CallbackContext> onSubmit;
    public void OnSubmit(InputAction.CallbackContext context)
    {
        onSubmit?.Invoke(context);
    }

    public Action<InputAction.CallbackContext> onMENUCancel;
    public void OnCancel(InputAction.CallbackContext context)
    {
        onMENUCancel?.Invoke(context);
    }

    public void OnPoint(InputAction.CallbackContext context) { }

    public void OnClick(InputAction.CallbackContext context) { }

    public void OnScrollWheel(InputAction.CallbackContext context) { }

    public void OnMiddleClick(InputAction.CallbackContext context) { }

    public void OnRightClick(InputAction.CallbackContext context) { }

    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }

    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }
}
