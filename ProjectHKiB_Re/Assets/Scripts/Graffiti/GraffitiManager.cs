using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class GraffitiManager : MonoBehaviour
{
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

    public int MaxGP;
    public int GP;

    public bool IsGraffitiEnded => _graffitiTimer.IsCooltimeEnded;

    public void StartGraffiti(Transform transform)
    {
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
        // trigger special attack
    }
    public void EndGraffitiSkill()
    {
        EndGraffitiProgress();
        
        // trigger special skill
        GP = 0;
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

}
