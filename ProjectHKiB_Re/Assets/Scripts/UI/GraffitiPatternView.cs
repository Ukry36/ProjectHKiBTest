using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GraffitiPatternView : MonoBehaviour 
{
    public PlayerSkillDataSO playerSkillDataSO;

    [NaughtyAttributes.Button]
    public void SetPattern() => SetPattern(playerSkillDataSO.graffitiCodes[0]);
    public void SetPattern(GraffitiCode graffitiCode)
    {
        int width = graffitiCode.Width < transform.childCount ? graffitiCode.Width : transform.childCount;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform vertical = transform.GetChild(i);
            vertical.gameObject.SetActive(i < width);
            if (i < width)
            {
                int height = graffitiCode.Height < vertical.childCount ? graffitiCode.Height : vertical.childCount;
                
                for (int j = 0; j < vertical.childCount; j++)
                {
                    Transform pix = vertical.GetChild(j);
                    pix.gameObject.SetActive(j < height);
                    if (j < height)
                    {
                        Image img = pix.GetComponent<Image>();
                        img.color = graffitiCode.color;
                        img.enabled = false;
                    }
                }
            }
        }

        for (int i = 0; i < graffitiCode.code.Count; i++)
        {
            Vector2 coord = graffitiCode.code[i];
            if ((int)coord.x >= transform.childCount) continue;
            Transform vertical = transform.GetChild((int)coord.x);
            if ((int)coord.y >= vertical.childCount) continue;
            Image img = vertical.GetChild((int)coord.y).GetComponent<Image>();
            img.enabled = true;
        }
    }
}