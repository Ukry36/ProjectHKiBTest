using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

public class GraffitiManager : MonoBehaviour
{
    public Player player;
    [SerializeField] private List<Vector2Int> graffitiProgress = new();
    private Vector2 _graffitiWorldStartPos;
    private Vector2 GraffitiWorldPos => _graffitiWorldStartPos + graffitiCurrentIntPos;
    private Vector2Int graffitiCurrentIntPos;
    private int graffitiMoveCount;
    private bool canGraffitiTinker;
    public GearManager gearManager;
    public InputManager inputManager;
    private Cooltime _graffitiTimer = new();
    public float graffitiMaxTime = 6;
    public SimpleAnimationPlayer[] tinkers;

    private Cooltime _GPRecoverTimer = new();
    public float GPRecovertime = 10;

    public int MaxGP = 5;
    [SerializeField]private int _GP;
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

    public bool CanGraffiti => GP > 0;

    public void Start()
    {
        Initialize();
    }
    public void Oestroy()
    {
        UnBindInputs();
    }

    public void Initialize()
    {
        _GP = MaxGP;
        StartGPRecoverTimer();
        BindInputs();
    }
    public void RecoverGP() => GP++;
    public void StartGPRecoverTimer()
    {
        if(_GP == MaxGP || !_GPRecoverTimer.IsCooltimeEnded) return;
        _GPRecoverTimer.CancelCooltime();
        _GPRecoverTimer.StartCooltime(10, RecoverGP);
    }

    public void StartGraffiti(Vector2 startPos)
    {
        if (!CanGraffiti) return;
        inputManager.GRAFFITIMode();
        _graffitiWorldStartPos = startPos;
        graffitiProgress.Clear();
        _graffitiTimer.StartCooltime(graffitiMaxTime, EndGraffitiTime);
        GP--;
        graffitiMoveCount = 0;
        canGraffitiTinker = false;
        
        ProcessGraffiti(Vector2Int.zero);
    }
    public void ResetGraffiti()
    {
        graffitiProgress.Clear();
        graffitiMoveCount = 0;
        
        ProcessGraffiti(Vector2Int.zero);
    }

    public void StartTinker()
    {
        if (canGraffitiTinker) return;
        canGraffitiTinker = true;
        for (int i = 0; i < graffitiMoveCount; i++)
            GraffitiTinker(i, _graffitiWorldStartPos + graffitiProgress[i]);
        if (CheckCompleted() >= 0)
            GraffitiTinkerComplete();
    }

    /// <summary>
    /// called everytime when moved in graffiti
    /// </summary>
    /// <param name="pos"> current position</param>
    public void ProcessGraffiti(Vector2Int pos) 
    {
        graffitiCurrentIntPos = pos;

        if (!graffitiProgress.Contains(pos)) 
        {
            graffitiProgress.Add(pos);
            GraffitiTinker(graffitiMoveCount, GraffitiWorldPos);
            graffitiMoveCount++;
        }
        else GraffitiTinker(graffitiProgress.IndexOf(pos), GraffitiWorldPos);

        if(CheckCompleted() >= 0)
        {
            GraffitiTinkerComplete();//success feedback
            return;
        }

        if (!ValidateProgress())
        {
            //fail feedback
            //graffitiProgress.Clear();
        }
    }

    public void GraffitiTinker(int animatorIndex, Vector2 pos)
    {
        if (canGraffitiTinker && animatorIndex < tinkers.Length && tinkers[animatorIndex])
        {
            tinkers[animatorIndex].transform.position = pos;
            tinkers[animatorIndex].Play("NormalStart");
            tinkers[animatorIndex].Reserve("NormalIdle");
        }
    }

    private Sequence sequence;
    private int tempTinkerIndex;
    public void GraffitiTinkerComplete()
    {
        if (!canGraffitiTinker) return;
        sequence = DOTween.Sequence();
        for (int i = 0; i < graffitiMoveCount; i++)
        {
            sequence.AppendCallback(BiggerTinkerCallback);
            sequence.AppendInterval(0.05f);
        }
        tempTinkerIndex = 0;
        sequence.Play();
    }

    private void BiggerTinkerCallback()
    {
        tinkers[tempTinkerIndex].ClearReservation();
        tinkers[tempTinkerIndex].Play("NormalExit");
        tinkers[tempTinkerIndex].Reserve("BiggerStart");
        tinkers[tempTinkerIndex].Reserve("BiggerIdle");
        tempTinkerIndex++;
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
        StartTinker();
        _graffitiTimer.CancelCooltime();
        if (CheckCompleted() >= 0) gearManager.EquipCard(CheckCompleted());
        
        graffitiProgress.Clear();
        inputManager.PLAYMode();
        
        if (sequence != null && sequence.active) sequence.Complete();
        for (int i = 0; i < graffitiMoveCount; i++) 
        {
            //tinkers[i].ClearReservation();
            if (CheckCompleted() >= 0) tinkers[i].Reserve("BiggerExit");
            else                       tinkers[i].Reserve("NormalExit");
            tinkers[i].Reserve("Stop");
        }
    }
    
    public void EndGraffitiAttack()
    {
        EndGraffitiProgress();
        StartCoroutine(EndGraffitiAttackCoroutine());
    }
    private IEnumerator EndGraffitiAttackCoroutine()
    {
        yield return null;
        player.ChangeState(player.BaseData.StateMachine.initialState);
        if (player.BaseData.GraffitiAttackState != null)
            player.ChangeState(player.BaseData.GraffitiAttackState);
    }
    public void EndGraffitiSkill()
    {
        GP = 0;
        EndGraffitiProgress();
        StartCoroutine(EndGraffitiSkillCoroutine());
    }
    private IEnumerator EndGraffitiSkillCoroutine()
    {
        yield return null;
        player.ChangeState(player.BaseData.StateMachine.initialState);
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
                if (graffitiCode.Intersect(graffitiProgress).ToList().Count == graffitiProgress.Count) 
                    return true;
            }
        }
        return false;
    }
    
    int CheckCompleted()
    {
        for(int cardNum = 0; cardNum < gearManager.MaxCardCount; cardNum++)
        {
            CardData card = gearManager.GetCardData(cardNum);
            if (card == null) continue;
            GearDataSO gear = card.mergedGearList.GetSafe(0);
            if (!gear || gear == gearManager.DefaultGearData) continue; 
            for (int i = 0; i < gear.graffitiAllCases.Count; i++)
            {
                List<Vector2Int> graffitiCode = gear.graffitiAllCases[i].code;
                if (graffitiCode.OrderBy(x => x.x).ThenBy(y => y.y).SequenceEqual(graffitiProgress.OrderBy(x => x.x).ThenBy(y => y.y)))
                    return cardNum;
            }
        }
        return -1;
    }

    private void BindInputs()
    {
        inputManager.Bind(EnumManager.InputType.OnGraffitiMoveDown, ProcessGraffitiDown);
        inputManager.Bind(EnumManager.InputType.OnGraffitiMoveLeft, ProcessGraffitiLeft);
        inputManager.Bind(EnumManager.InputType.OnGraffitiMoveRight, ProcessGraffitiRight);
        inputManager.Bind(EnumManager.InputType.OnGraffitiMoveUp, ProcessGraffitiUp);
        inputManager.Bind(EnumManager.InputType.OnGraffitiAttack, EndGraffitiAttack);
        inputManager.Bind(EnumManager.InputType.OnGraffitiSkill, EndGraffitiSkill);
        inputManager.Bind(EnumManager.InputType.OnGraffitiReset, ResetGraffiti);
        inputManager.Bind(EnumManager.InputType.OnGraffitiCancel, CancelGraffiti);
    }

    private void UnBindInputs()
    {
        inputManager.UnBind(EnumManager.InputType.OnGraffitiMoveDown, ProcessGraffitiDown);
        inputManager.UnBind(EnumManager.InputType.OnGraffitiMoveLeft, ProcessGraffitiLeft);
        inputManager.UnBind(EnumManager.InputType.OnGraffitiMoveRight, ProcessGraffitiRight);
        inputManager.UnBind(EnumManager.InputType.OnGraffitiMoveUp, ProcessGraffitiUp);
        inputManager.UnBind(EnumManager.InputType.OnGraffitiAttack, EndGraffitiAttack);
        inputManager.UnBind(EnumManager.InputType.OnGraffitiSkill, EndGraffitiSkill);
        inputManager.UnBind(EnumManager.InputType.OnGraffitiReset, ResetGraffiti);
        inputManager.UnBind(EnumManager.InputType.OnGraffitiCancel, CancelGraffiti);
    }

    public void ProcessGraffitiDown(InputAction.CallbackContext context) { if (context.performed) ProcessGraffiti(graffitiCurrentIntPos + Vector2Int.down); }
    public void ProcessGraffitiLeft(InputAction.CallbackContext context) { if (context.performed) ProcessGraffiti(graffitiCurrentIntPos + Vector2Int.left); }
    public void ProcessGraffitiRight(InputAction.CallbackContext context) { if (context.performed) ProcessGraffiti(graffitiCurrentIntPos + Vector2Int.right); }
    public void ProcessGraffitiUp(InputAction.CallbackContext context) { if (context.performed) ProcessGraffiti(graffitiCurrentIntPos + Vector2Int.up); }
    public void EndGraffitiAttack(InputAction.CallbackContext context) { if (context.performed) EndGraffitiAttack(); }
    public void EndGraffitiSkill(InputAction.CallbackContext context) { if (context.performed) EndGraffitiSkill(); }
    public void ResetGraffiti(InputAction.CallbackContext context) { if (context.performed) ResetGraffiti(); }
    public void CancelGraffiti(InputAction.CallbackContext context) { if (context.performed) EndGraffitiProgress(); }

}
