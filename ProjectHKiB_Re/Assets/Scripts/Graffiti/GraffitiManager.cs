using System.Collections.Generic;
using UnityEngine;

public class GraffitiManager : MonoBehaviour
{
    private List<Vector2> graffitiProgress = new();
    private Vector2 graffitiStartPos;
    public GearManager gearManager;
    public InputManager inputManager;
    public Cooltime graffitiTimer;
    public float graffitiMaxTime = 6;

    public void StartGraffiti(Transform transform)
    {
        inputManager.GRAFFITIMode();
        graffitiStartPos = transform.position;
        graffitiProgress.Clear();
        graffitiProgress.Add(graffitiStartPos);
        graffitiTimer = new();
        graffitiTimer.StartCooltime(graffitiMaxTime, EndGraffitiNormal);
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

    public void EndGraffitiNormal()
    {
        gearManager.EquipCard(CheckCompleted());
        graffitiProgress.Clear();
        inputManager.PLAYMode();
    }
    public void EndGraffitiAttack()
    {
        EndGraffitiNormal();
        // takes graffiti guage
        // trigger special attack
    }
    public void EndGraffitiSkill()
    {
        EndGraffitiNormal();
        // takes graffiti guage
        // trigger special skill
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
