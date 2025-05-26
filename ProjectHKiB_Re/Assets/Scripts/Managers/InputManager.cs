using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
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
    public bool UIConfirmInput { get; private set; }
    public bool PauseInput { get; private set; }
    public bool EquipmentOpenCloseInput { get; private set; }
    public bool InventoryOpenCloseInput { get; private set; }

    public bool NextInput { get; private set; }
    public bool PrevInput { get; private set; }
    public bool CancelInput { get; private set; }
    public bool SprintInput { get; private set; }

    private PlayerInput _playerInput;
    private InputAction move, movePressedD, movePressedR, movePressedU, movePressedL,
            sprint, attack, dodge, grafitti, skill, confirm, cancel, equipment, inventory;

    public bool stopPlayerMovement;
    public bool stopPlayer;
    public bool stopUI;

    private void Awake()
    {
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
    }

    public void StopPlayerInput(bool _stop)
    {
        stopPlayer = _stop;
        MoveInput = Vector2.zero;
        SprintInput = false;
        AttackInput = false;
        DodgeInput = false;
        DodgeProgressInput = false;
        GraffitiStartInput = false;
        GraffitiEndInput = false;
        SkillInput = false;
        ConfirmInput = false;
    }

    public void StopUIInput(bool _stop)
    {
        stopUI = _stop;
        PauseInput = false;
        ConfirmInput = false;
        CancelInput = false;
        EquipmentOpenCloseInput = false;
        InventoryOpenCloseInput = false;
    }

    public void StopPlayerMovementInput(bool _stop)
    {
        stopPlayerMovement = _stop;
        MoveInput = Vector2.zero;
    }


    private void Update()
    {
        // player input detect
        if (!stopPlayer || !stopPlayerMovement)
            if (!stopPlayer)
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
            }

        // ui input detect 
        if (!stopUI)
        {
            PauseInput = cancel.WasPressedThisFrame();
            UIConfirmInput = confirm.WasPressedThisFrame();
            EquipmentOpenCloseInput = equipment.WasPressedThisFrame();
            InventoryOpenCloseInput = inventory.WasPressedThisFrame();
        }

        SprintInput = sprint.inProgress;
        CancelInput = cancel.WasPressedThisFrame();
        NextInput = equipment.WasPressedThisFrame();
        PrevInput = inventory.WasPressedThisFrame();
    }

    public bool GetInputByEnum(EnumManager.InputType inputType)
    {
        return inputType switch
        {
            EnumManager.InputType.OnMove => !MoveInput.Equals(Vector2.zero),
            EnumManager.InputType.OnSprint => SprintInput,
            EnumManager.InputType.OnAttack => AttackInput,
            EnumManager.InputType.OnDodge => DodgeInput,
            EnumManager.InputType.HasDInput => MoveInput.y < 0,
            EnumManager.InputType.HasLInput => MoveInput.x < 0,
            EnumManager.InputType.HasRInput => MoveInput.x > 0,
            EnumManager.InputType.HasUInput => MoveInput.y > 0,
            _ => false,
        };
    }
}
