using System.Collections.Generic;
using UnityEngine;

public class GraffitiManager : MonoBehaviour
{
    private List<Vector2> graffitiProgress = new();
    private Vector2 graffitiStartPos;
    public GearManager gearManager;
    public InputManager inputManager;
    private Cooltime _graffitiTimer = new();
    public float graffitiMaxTime = 6;

    public int MaxGP;
    public int GP;

    public bool IsGraffitiEnded => _graffitiTimer.IsCooltimeEnded;

    public void StartGraffiti(Transform transform)
    {
        inputManager.GRAFFITIMode();
        graffitiStartPos = transform.position;
        graffitiProgress.Clear();
        graffitiProgress.Add(graffitiStartPos);
        _graffitiTimer.StartCooltime(graffitiMaxTime, EndGraffitiNormal);
        GP--;
    }

    /// <summary>
    /// called everytime when player moves in graffiti
    /// </summary>
    /// <param name="playerPos"> current player pos</param>
    public void ProcessGraffiti(Vector2 playerPos) 
    {  
        Vector2 currentPos = playerPos - graffitiStartPos;
        graffitiProgress.Add(Vector2.right * Mathf.RoundToInt(currentPos.x) + Vector2.up * Mathf.RoundToInt(currentPos.y));

        if(CheckCompleted() >= 0)
        {
            //success feedback
            return;
        }

        if (!ValidateProgress())
        {
            //fail feedback
            graffitiProgress.Clear();
        }
    }
    
    public void EndGraffitiTime()
    {
        if (CheckCompleted() >= 0)
            EndGraffitiAttack();
        else
            EndGraffitiNormal();
    }
    public void EndGraffitiNormal()
    {
        _graffitiTimer.CancelCooltime();
        gearManager.EquipCard(CheckCompleted());
        graffitiProgress.Clear();
        inputManager.PLAYMode();
    }
    public void EndGraffitiAttack()
    {
        EndGraffitiNormal();
        // trigger special attack
    }
    public void EndGraffitiSkill()
    {
        EndGraffitiNormal();
        
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
                List<Vector2> graffitiCode = gear.graffitiAllCases[i].code;
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
                List<Vector2> graffitiCode = gear.graffitiAllCases[i].code;
                if (graffitiCode == graffitiProgress)
                    return cardNum;
            }
        }
        return -1;
    }

}
