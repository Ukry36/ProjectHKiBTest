using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class GraffitiManager : MonoBehaviour
{
    public Player player;
    private List<Vector2Int> graffitiProgress = new();
    private Vector2 _graffitiWorldStartPos;
    private Vector2 GraffitiWorldPos => _graffitiWorldStartPos + graffitiCurrentIntPos;
    private Vector2Int graffitiCurrentIntPos;
    private int graffitiMoveCount;
    private bool isGraffitiInProgress;
    public GearManager gearManager;
    public InputManager inputManager;
    private Cooltime _graffitiTimer = new();
    public float graffitiMaxTime = 6;
    public SimpleAnimationPlayer[] tinkers;

    private Cooltime _GPRecoverTimer = new();
    public float GPRecovertime = 10;

    public int MaxGP = 5;
    private int _GP;
    public int GP 
    { 
        get => _GP;
        set
        {
            _GP = value;
            if (_GP >= MaxGP && !_GPRecoverTimer.IsCooltimeEnded) _GPRecoverTimer.CancelCooltime();
            if (_GP > MaxGP) _GP = MaxGP;
            else 
            {
                if (_GP < 0) _GP = 0;
                if (_GPRecoverTimer.IsCooltimeEnded) StartGPRecoverTimer();
            }
        }
    }

    public bool IsGraffitiEnded => _graffitiTimer.IsCooltimeEnded;

    public void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        _GP = MaxGP;
        StartGPRecoverTimer();
    }
    public void RecoverGP() => GP++;
    public void StartGPRecoverTimer()
    {
        if(_GP == MaxGP || !_GPRecoverTimer.IsCooltimeEnded) return;
        _GPRecoverTimer.CancelCooltime();
        _GPRecoverTimer.StartCooltime(10, RecoverGP);
    }

    public void StartGraffiti(Transform transform)
    {
        if (GP < 1) return;
        isGraffitiInProgress = true;
        inputManager.GRAFFITIMode();
        _graffitiWorldStartPos = transform.position;
        graffitiProgress.Clear();
        graffitiProgress.Add(Vector2Int.zero);
        _graffitiTimer.StartCooltime(graffitiMaxTime, EndGraffitiProgress);
        GP--;
        graffitiMoveCount = 0;
        
        UnBindInputs();
        BindInputs();
    }

    /// <summary>
    /// called everytime when moved in graffiti
    /// </summary>
    /// <param name="pos"> current position</param>
    public void ProcessGraffiti(Vector2Int pos) 
    {
        if (!isGraffitiInProgress) return;

        if (!graffitiProgress.Contains(pos)) graffitiProgress.Add(pos);
        graffitiCurrentIntPos = pos;
        
        GraffitiTinker();
        graffitiMoveCount++;

        if(CheckCompleted() >= 0)
        {
            GraffitiTinkerComplete();//success feedback
            return;
        }

        if (!ValidateProgress())
        {
            //fail feedback
            graffitiProgress.Clear();
        }
    }

    public void GraffitiTinker()
    {
        if (graffitiMoveCount < tinkers.Length && tinkers[graffitiMoveCount])
        {
            tinkers[graffitiMoveCount].transform.position = GraffitiWorldPos;
            tinkers[graffitiMoveCount].Play("NormalStart");
            tinkers[graffitiMoveCount].Reserve("NormalIdle");
        }
    }

    public void GraffitiTinkerComplete()
    {
        Sequence sequence = DOTween.Sequence();
        for (int i = 0; i < graffitiMoveCount; i++)
        {
            sequence.AppendCallback(BiggerTinkerCallback);
            sequence.AppendInterval(0.02f);
        }
        sequence.Play();
    }

    private void BiggerTinkerCallback()
    {
        tinkers[graffitiMoveCount].Play("BiggerStart");
        tinkers[graffitiMoveCount].Reserve("BiggerIdle");
    }
    
    public void EndGraffitiTime()
    {
        if (CheckCompleted() >= 0)
            EndGraffitiAttack();
        else
            EndGraffitiProgress();
    }
    public void EndGraffitiProgress()
    {
        _graffitiTimer.CancelCooltime();
        gearManager.EquipCard(CheckCompleted());
        graffitiProgress.Clear();
        inputManager.PLAYMode();
        UnBindInputs();
        for (int i = 0; i < graffitiMoveCount; i++) { tinkers[graffitiMoveCount].Stop(); }
        
        isGraffitiInProgress = false;
    }
    
    public void EndGraffitiAttack()
    {
        EndGraffitiProgress();
        if (player.BaseData.GraffitiAttackState != null)
            player.ChangeState(player.BaseData.GraffitiAttackState);
    }
    public void EndGraffitiSkill()
    {
        GP = 0;
        EndGraffitiProgress();
        if (player.BaseData.GraffitiSkillState != null)
            player.ChangeState(player.BaseData.GraffitiSkillState);
    }

    private bool ValidateProgress()
    {
        for(int cardNum = 0; cardNum < gearManager.MaxCardCount; cardNum++)
        {
            CardData card = gearManager.GetCardData(cardNum);
            if (card != null) continue;
            GearDataSO gear = card.mergedGearList.GetSafe(cardNum);
            if (!gear || gear == gearManager.DefaultGearData) continue; 
            for (int i = 0; i < gear.graffitiAllCases.Count; i++)
            {
                List<Vector2Int> graffitiCode = gear.graffitiAllCases[i].code;
                bool isSkillValid = true;
                for (int j = 0; j < graffitiProgress.Count; j++)
                {
                    if (!graffitiCode.Contains(graffitiProgress[j]))
                    {
                        isSkillValid = false;
                        break;
                    }
                }
                if (isSkillValid) return true;
            }
        }
        return false;
    }
    
    int CheckCompleted()
    {
        for(int cardNum = 0; cardNum < gearManager.MaxCardCount; cardNum++)
        {
            CardData card = gearManager.GetCardData(cardNum);
            if (card != null) continue;
            GearDataSO gear = card.mergedGearList.GetSafe(cardNum);
            if (!gear || gear == gearManager.DefaultGearData) continue; 
            for (int i = 0; i < gear.graffitiAllCases.Count; i++)
            {
                List<Vector2Int> graffitiCode = gear.graffitiAllCases[i].code;
                if (graffitiCode == graffitiProgress)
                    return cardNum;
            }
        }
        return -1;
    }

    private void BindInputs()
    {
        inputManager.Bind(EnumManager.InputType.OnMoveDown, ProcessGraffitiDown);
        inputManager.Bind(EnumManager.InputType.OnMoveLeft, ProcessGraffitiLeft);
        inputManager.Bind(EnumManager.InputType.OnMoveRight, ProcessGraffitiRight);
        inputManager.Bind(EnumManager.InputType.OnMoveUp, ProcessGraffitiUp);
        inputManager.Bind(EnumManager.InputType.OnAttack, EndGraffitiAttack);
        inputManager.Bind(EnumManager.InputType.OnSkill, EndGraffitiSkill);
    }

    private void UnBindInputs()
    {
        inputManager.UnBind(EnumManager.InputType.OnMoveDown, ProcessGraffitiDown);
        inputManager.UnBind(EnumManager.InputType.OnMoveLeft, ProcessGraffitiLeft);
        inputManager.UnBind(EnumManager.InputType.OnMoveRight, ProcessGraffitiRight);
        inputManager.UnBind(EnumManager.InputType.OnMoveUp, ProcessGraffitiUp);
        inputManager.UnBind(EnumManager.InputType.OnAttack, EndGraffitiAttack);
        inputManager.UnBind(EnumManager.InputType.OnSkill, EndGraffitiSkill);
    }

    public void ProcessGraffitiDown(InputAction.CallbackContext context) { if (context.performed) ProcessGraffiti(graffitiCurrentIntPos + Vector2Int.down); }
    public void ProcessGraffitiLeft(InputAction.CallbackContext context) { if (context.performed) ProcessGraffiti(graffitiCurrentIntPos + Vector2Int.left); }
    public void ProcessGraffitiRight(InputAction.CallbackContext context) { if (context.performed) ProcessGraffiti(graffitiCurrentIntPos + Vector2Int.right); }
    public void ProcessGraffitiUp(InputAction.CallbackContext context) { if (context.performed) ProcessGraffiti(graffitiCurrentIntPos + Vector2Int.up); }
    public void EndGraffitiAttack(InputAction.CallbackContext context) { if (context.performed) EndGraffitiAttack(); }
    public void EndGraffitiSkill(InputAction.CallbackContext context) { if (context.performed) EndGraffitiSkill(); }

}
